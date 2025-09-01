using Larnix.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using Larnix.Blocks;

namespace Larnix.Server.Entities
{
    public class EntityDataManager : MonoBehaviour
    {
        // Every EntityData entry must be controlled by one specific EntityController object
        private readonly Dictionary<ulong, EntityData> EntityData = new Dictionary<ulong, EntityData>();

        private readonly Dictionary<ulong, EntityData> UnloadedEntityData = new Dictionary<ulong, EntityData>();
        private readonly Dictionary<ulong, EntityData> DeletedEntityData = new Dictionary<ulong, EntityData>();

        private void Awake()
        {
            References.EntityDataManager = this;
        }

        public EntityData TryFindEntityData(ulong uid)
        {
            if (DeletedEntityData.ContainsKey(uid))
                return null;

            if (UnloadedEntityData.ContainsKey(uid))
                return UnloadedEntityData[uid];

            if(EntityData.ContainsKey(uid))
                return EntityData[uid];

            return References.Server.Database.FindEntity(uid);
        }

        public Dictionary<ulong, EntityData> GetUnloadedEntitiesByChunk(Vector2Int chunkCoords)
        {
            Dictionary<ulong, EntityData> entityList = References.Server.Database.GetEntitiesByChunkNoPlayers(chunkCoords);

            foreach(var vkp in EntityData)
            {
                if(entityList.ContainsKey(vkp.Key)) // already loaded entity
                    entityList.Remove(vkp.Key);
            }

            foreach (var vkp in DeletedEntityData)
            {
                if (entityList.ContainsKey(vkp.Key)) // entity already removed
                    entityList.Remove(vkp.Key);
            }

            foreach (var vkp in UnloadedEntityData)
            {
                EntityData newData = vkp.Value;
                Vector2Int newChunkCoords = ChunkMethods.CoordsToChunk(newData.Position);
                bool in_the_chunk = (newChunkCoords == chunkCoords);

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
            if (UnloadedEntityData.ContainsKey(uid))
                UnloadedEntityData.Remove(uid);

            if (DeletedEntityData.ContainsKey(uid))
                DeletedEntityData.Remove(uid);

            EntityData[uid] = entityData;
        }

        public void UnloadEntityData(ulong uid)
        {
            if (EntityData.ContainsKey(uid))
            {
                UnloadedEntityData.Add(uid, EntityData[uid]);
                EntityData.Remove(uid);
            }
        }

        public void DeleteEntityData(ulong uid)
        {
            if (EntityData.ContainsKey(uid))
            {
                DeletedEntityData.Add(uid, EntityData[uid]);
                EntityData.Remove(uid);
            }
        }

        public void FlushIntoDatabase()
        {
            References.Server.Database.DeleteEntities(GetKeyList(DeletedEntityData));
            DeletedEntityData.Clear();

            References.Server.Database.FlushEntities(MergeDictionaries(EntityData, UnloadedEntityData));
            UnloadedEntityData.Clear();
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
