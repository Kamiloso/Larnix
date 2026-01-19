using System.Collections;
using System.Collections.Generic;
using Larnix.Entities;
using Larnix.Blocks;
using Larnix.Core.Utils;
using Larnix.Entities.Structs;
using Larnix.Server.Data;
using Larnix.Server.References;
using Larnix.Core.Vectors;

namespace Larnix.Server.Entities
{
    internal class EntityDataManager : ServerSingleton
    {
        private readonly Dictionary<ulong, EntityData> entityData = new Dictionary<ulong, EntityData>();
        private readonly Dictionary<ulong, EntityData> unloadedEntityData = new Dictionary<ulong, EntityData>();
        private readonly Dictionary<ulong, EntityData> deletedEntityData = new Dictionary<ulong, EntityData>();

        public EntityDataManager(Server server) : base(server) { }

        public EntityData TryFindEntityData(ulong uid)
        {
            if (deletedEntityData.ContainsKey(uid))
                return null;

            if (unloadedEntityData.ContainsKey(uid))
                return unloadedEntityData[uid];

            if(entityData.ContainsKey(uid))
                return entityData[uid];

            return Ref<Database>().FindEntity(uid);
        }

        public Dictionary<ulong, EntityData> GetUnloadedEntitiesByChunk(Vec2Int chunkCoords)
        {
            Dictionary<ulong, EntityData> entityList = Ref<Database>().GetEntitiesByChunkNoPlayers(chunkCoords);

            foreach(var vkp in entityData)
            {
                if(entityList.ContainsKey(vkp.Key)) // already loaded entity
                    entityList.Remove(vkp.Key);
            }

            foreach (var vkp in deletedEntityData)
            {
                if (entityList.ContainsKey(vkp.Key)) // entity already removed
                    entityList.Remove(vkp.Key);
            }

            foreach (var vkp in unloadedEntityData)
            {
                EntityData newData = vkp.Value;
                Vec2Int newChunkCoords = BlockUtils.CoordsToChunk(newData.Position);
                bool in_the_chunk = newChunkCoords == chunkCoords;

                if (entityList.ContainsKey(vkp.Key))
                {
                    if(in_the_chunk)
                        entityList[vkp.Key] = newData; // update existing data

                    if(!in_the_chunk)
                        entityList.Remove(vkp.Key); // no longer in this chunk
                }
                else
                {
                    if(in_the_chunk)
                    {
                        if(newData.ID != EntityID.Player)
                            entityList.Add(vkp.Key, newData); // additional data found
                    }
                }
            }

            return entityList;
        }

        public void SetEntityData(ulong uid, EntityData entityData)
        {
            if (unloadedEntityData.ContainsKey(uid))
                unloadedEntityData.Remove(uid);

            if (deletedEntityData.ContainsKey(uid))
                deletedEntityData.Remove(uid);

            this.entityData[uid] = entityData;
        }

        public void UnloadEntityData(ulong uid)
        {
            if (entityData.ContainsKey(uid))
            {
                unloadedEntityData.Add(uid, entityData[uid]);
                entityData.Remove(uid);
            }
        }

        public void DeleteEntityData(ulong uid)
        {
            if (entityData.ContainsKey(uid))
            {
                deletedEntityData.Add(uid, entityData[uid]);
                entityData.Remove(uid);
            }
        }

        public void FlushIntoDatabase()
        {
            Ref<Database>().DeleteEntities(GetKeyList(deletedEntityData));
            deletedEntityData.Clear();

            Ref<Database>().FlushEntities(MergeDictionaries(entityData, unloadedEntityData));
            unloadedEntityData.Clear();
        }

        private static Dictionary<ulong, EntityData> MergeDictionaries(
            Dictionary<ulong, EntityData> first,
            Dictionary<ulong, EntityData> second
            )
        {
            var result = new Dictionary<ulong, EntityData>();
            foreach(var kvp in first)
            {
                result[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in second)
            {
                if (!result.ContainsKey(kvp.Key))
                    result[kvp.Key] = kvp.Value;
                else
                    throw new System.InvalidOperationException("Dictionaries are not distinct!");
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
