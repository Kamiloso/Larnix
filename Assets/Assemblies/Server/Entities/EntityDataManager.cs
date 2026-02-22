using System.Collections;
using System.Collections.Generic;
using Larnix.Entities;
using Larnix.Core.Utils;
using Larnix.Entities.Structs;
using Larnix.Server.Data;
using Larnix.Core.Vectors;
using System;

namespace Larnix.Server.Entities
{
    internal class EntityDataManager : IScript
    {
        private readonly Dictionary<ulong, EntityData> _entityData = new();
        private readonly Dictionary<ulong, EntityData> _unloadedEntityData = new();
        private readonly Dictionary<ulong, EntityData> _deletedEntityData = new();

        private Database Database => Ref.Database;

        public EntityData TryFindEntityData(ulong uid)
        {
            if (_deletedEntityData.ContainsKey(uid))
                return null;

            if (_unloadedEntityData.ContainsKey(uid))
                return _unloadedEntityData[uid];

            if(_entityData.ContainsKey(uid))
                return _entityData[uid];

            return Database.FindEntity(uid);
        }

        public Dictionary<ulong, EntityData> GetUnloadedEntitiesByChunk(Vec2Int chunkCoords)
        {
            Dictionary<ulong, EntityData> entityList = Database.GetEntitiesByChunkNoPlayers(chunkCoords);

            foreach(var kvp in _entityData)
            {
                if(entityList.ContainsKey(kvp.Key)) // already loaded entity
                    entityList.Remove(kvp.Key);
            }

            foreach (var kvp in _deletedEntityData)
            {
                if (entityList.ContainsKey(kvp.Key)) // entity already removed
                    entityList.Remove(kvp.Key);
            }

            foreach (var kvp in _unloadedEntityData)
            {
                EntityData newData = kvp.Value;
                Vec2Int newChunkCoords = BlockUtils.CoordsToChunk(newData.Position);
                bool in_the_chunk = newChunkCoords == chunkCoords;

                if (entityList.ContainsKey(kvp.Key))
                {
                    if(in_the_chunk)
                        entityList[kvp.Key] = newData; // update existing data

                    if(!in_the_chunk)
                        entityList.Remove(kvp.Key); // no longer in this chunk
                }
                else
                {
                    if(in_the_chunk)
                    {
                        if(newData.ID != EntityID.Player)
                            entityList.Add(kvp.Key, newData); // additional data found
                    }
                }
            }

            return entityList;
        }

        public void SetEntityData(ulong uid, EntityData entityData)
        {
            if (_unloadedEntityData.ContainsKey(uid))
                _unloadedEntityData.Remove(uid);

            if (_deletedEntityData.ContainsKey(uid))
                _deletedEntityData.Remove(uid);

            _entityData[uid] = entityData;
        }

        public void UnloadEntityData(ulong uid)
        {
            if (_entityData.ContainsKey(uid))
            {
                _unloadedEntityData.Add(uid, _entityData[uid]);
                _entityData.Remove(uid);
            }
        }

        public void DeleteEntityData(ulong uid)
        {
            if (_entityData.ContainsKey(uid))
            {
                _deletedEntityData.Add(uid, _entityData[uid]);
                _entityData.Remove(uid);
            }
        }

        public void FlushIntoDatabase()
        {
            Database.DeleteEntities(GetKeyList(_deletedEntityData));
            _deletedEntityData.Clear();

            Database.FlushEntities(MergeDictionaries(_entityData, _unloadedEntityData));
            _unloadedEntityData.Clear();
        }

        private static Dictionary<ulong, EntityData> MergeDictionaries(
            Dictionary<ulong, EntityData> first,
            Dictionary<ulong, EntityData> second
            )
        {
            Dictionary<ulong, EntityData> result = new();
            foreach(var kvp in first)
            {
                result[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in second)
            {
                if (!result.ContainsKey(kvp.Key))
                    result[kvp.Key] = kvp.Value;
                else
                    throw new InvalidOperationException("Dictionaries are not distinct!");
            }
            return result;
        }

        private static List<ulong> GetKeyList(Dictionary<ulong, EntityData> dict)
        {
            var result = new List<ulong>();
            foreach (var kvp in dict)
            {
                result.Add(kvp.Key);
            }
            return result;
        }
    }
}
