using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using System.Linq;
using Larnix.Socket;
using Larnix.Socket.Commands;

namespace Larnix.Server
{
    public class EntityManager : MonoBehaviour
    {
        private readonly Dictionary<string, EntityController> PlayerControllers = new Dictionary<string, EntityController>();
        private readonly Dictionary<ulong, EntityController> EntityControllers = new Dictionary<ulong, EntityController>();

        private uint FixedCounter = 0;
        private uint LastSentFixedCounter = 0;

        private void Awake()
        {
            References.EntityManager = this;
        }

        private void FixedUpdate()
        {
            // Fixed counter increment

            FixedCounter++;

            // Execute entity behaviours

            foreach (var controller in EntityControllers.Values)
                if(controller.gameObject.activeSelf && controller.EntityData.ID != EntityID.Player)
                {
                    controller.FromFixedUpdate();
                }

            // Kill entities when needed

            foreach(ulong uid in EntityControllers.Keys.ToList())
                if (EntityControllers[uid].gameObject.activeSelf)
                {
                    if (EntityControllers[uid].EntityData.NBT == "something... idk")
                        KillEntity(uid);
                }
        }

        public void FromEarlyUpdate() // 2
        {
            // Unload entities

            foreach(ulong uid in EntityControllers.Keys.ToList())
                if (EntityControllers[uid].gameObject.activeSelf && EntityControllers[uid].EntityData.ID != EntityID.Player)
                {
                    if (!References.ChunkLoading.IsEntityInAliveZone(EntityControllers[uid]))
                        UnloadEntity(uid);
                }
        }

        public void SendEntityBroadcast()
        {
            if(FixedCounter != LastSentFixedCounter)
            {
                Dictionary<ulong, uint> FixedFrames = References.PlayerManager.GetFixedFramesByUID();
                List<string> connected_nicknames = References.PlayerManager.PlayerUID.Keys.ToList();

                foreach (string nickname in connected_nicknames)
                {
                    Vector2 playerPos = References.PlayerManager.GetPlayerRenderingPosition(nickname);

                    Dictionary<ulong, EntityData> EntityList = new();
                    Dictionary<ulong, uint> PlayerFixedIndexes = new();

                    const float MAX_DISTANCE = 16f * 3;
                    const int MAX_ENTITIES = 2048; // also in EntityBroadcast.cs
                    const int MAX_FIXED_INDEXES = 1024; // also in EntityBroadcast.cs

                    foreach (ulong uid in EntityControllers.Keys)
                    {
                        if (EntityList.Count >= MAX_ENTITIES)
                            break;

                        // -- checking entities to add --
                        EntityController entity = EntityControllers[uid];
                        Vector2 entityPos = entity.EntityData.Position;
                        bool lookingAtPlayer = entity.EntityData.ID == EntityID.Player;

                        if (!entity.gameObject.activeSelf)
                            continue; // sending only active entities, active players always have a recent update object

                        if (Vector2.Distance(playerPos, entityPos) < MAX_DISTANCE)
                        {
                            EntityList.Add(uid, entity.EntityData);

                            if (PlayerFixedIndexes.Count >= MAX_FIXED_INDEXES)
                                continue;

                            // -- adding indexes --
                            if(lookingAtPlayer)
                            {
                                PlayerFixedIndexes.Add(uid, FixedFrames[uid]);
                            }
                        }
                    }

                    EntityBroadcast entityBroadcast = new EntityBroadcast(
                        FixedCounter,
                        0.0, // relict
                        EntityList,
                        PlayerFixedIndexes
                        );
                    if (!entityBroadcast.HasProblems)
                    {
                        Packet packet = entityBroadcast.GetPacket();
                        References.Server.Send(nickname, packet, false); // unsafe mode (over raw UDP)
                    }
                }

                LastSentFixedCounter = FixedCounter;
            }
        }

        public void CreatePlayerController(string nickname)
        {
            EntityController playerController = EntityController.CreatePlayerController(nickname);
            PlayerControllers.Add(nickname, playerController);
            EntityControllers.Add(playerController.uID, playerController);
        }

        public EntityController GetPlayerController(string nickname)
        {
            if(PlayerControllers.ContainsKey(nickname))
                return PlayerControllers[nickname];
            return null;
        }

        public string GetNicknameByOnlineUID(ulong uid)
        {
            return PlayerControllers.FirstOrDefault(kvp => kvp.Value.uID == uid).Key;
        }

        public void UnloadPlayerController(string nickname)
        {
            EntityController playerController = PlayerControllers[nickname];
            PlayerControllers.Remove(nickname);
            EntityControllers.Remove(playerController.uID);
            playerController.UnloadEntityInstant();
        }

        public List<EntityController> GetAllPlayerControllers()
        {
            return PlayerControllers.Values.ToList();
        }

        public List<EntityController> GetAllEntityControllers()
        {
            return EntityControllers.Values.ToList();
        }

        public void SummonEntity(EntityData entityData)
        {
            EntityController entityController = EntityController.CreateNewEntityController(entityData);
            EntityControllers.Add(entityController.uID, entityController);
            entityController.ActivateIfNotActive();
        }

        public void LoadEntitiesByChunk(Vector2Int chunkCoords)
        {
            Dictionary<ulong, EntityData> entities = References.EntityDataManager.GetUnloadedEntitiesByChunk(chunkCoords);
            foreach (var vkp in entities)
            {
                EntityController entityController = EntityController.CreateExistingEntityController(vkp.Key, vkp.Value);
                EntityControllers.Add(entityController.uID, entityController);
                entityController.ActivateIfNotActive();
            }
        }

        private void UnloadEntity(ulong uid)
        {
            if (!EntityControllers.ContainsKey(uid))
                throw new System.InvalidOperationException("Entity with ID " + uid + " is not loaded!");

            EntityController entityController = EntityControllers[uid];
            if (entityController.EntityData.ID == EntityID.Player)
                throw new System.InvalidOperationException("Cannot unload player this way!");

            EntityControllers[uid].UnloadEntityInstant();
            EntityControllers.Remove(uid);
        }

        private void KillEntity(ulong uid)
        {
            if (!EntityControllers.ContainsKey(uid))
                throw new System.InvalidOperationException("Entity with ID " + uid + " is not loaded!");

            string nickname = GetNicknameByOnlineUID(uid);
            if(nickname != null)
                PlayerControllers.Remove(nickname);

            EntityControllers[uid].DeleteEntityInstant();
            EntityControllers.Remove(uid);
        }
    }
}
