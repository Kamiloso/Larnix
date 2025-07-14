using Larnix.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using Larnix.Socket.Commands;
using Larnix.Socket;

namespace Larnix.Server
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

        public void SendEntityBroadcast(uint seq)
        {
            EntityBroadcast entityBroadcast = new EntityBroadcast(
                seq,
                EntityData
                );
            if(!entityBroadcast.HasProblems) // Has problems if has over 2048 entities.
            {
                Packet packet = entityBroadcast.GetPacket();
                References.Server.Broadcast(packet, false); // unsafe mode (over raw UDP)
            }
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
