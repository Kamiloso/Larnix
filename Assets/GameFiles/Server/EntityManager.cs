using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using System.Linq;
using Larnix.Socket;
using Larnix.Socket.Commands;
using System.Diagnostics;

namespace Larnix.Server
{
    public class EntityManager : MonoBehaviour
    {
        private readonly Dictionary<string, EntityAbstraction> PlayerControllers = new();
        private readonly Dictionary<ulong, EntityAbstraction> EntityControllers = new();

        private uint FixedCounter = 0;
        private uint LastSentFixedCounter = 0;
        private uint UpdateCounter = 0; // just to check modulo when sending NearbyEntities packet

        private void Awake()
        {
            References.EntityManager = this;
        }

        private void FixedUpdate()
        {
            // Fixed counter increment

            FixedCounter++;

            // Execute entity behaviours

            // DON'T add .ToList() or anything like that here.
            // We need a clear error message when accessing this dictionary inproperly.
            foreach (var controller in EntityControllers.Values)
                if(controller.IsActive)
                {
                    controller.FromFixedUpdate();
                }

            // Kill entities when needed

            foreach(ulong uid in EntityControllers.Keys.ToList())
                if (EntityControllers[uid].IsActive)
                {
                    if (EntityControllers[uid].EntityData.NBT == "something... idk")
                        KillEntity(uid);
                }
        }

        public void FromEarlyUpdate() // 2
        {
            // Unload / Activate entities

            foreach(ulong uid in EntityControllers.Keys.ToList())
            {
                EntityAbstraction controller = EntityControllers[uid];
                if (controller.IsActive && controller.EntityData.ID != EntityID.Player)
                {
                    if (!References.ChunkLoading.IsEntityInAliveZone(controller))
                        UnloadEntity(uid);
                }
            }

            // Activate entities

            const double MAX_ACTIVATING_MS = 3.0f;
            Stopwatch timer = Stopwatch.StartNew();

            foreach (ulong uid in EntityControllers.Keys.ToList())
            {
                EntityAbstraction controller = EntityControllers[uid];
                if (!controller.IsActive && controller.EntityData.ID != EntityID.Player)
                {
                    if (timer.Elapsed.TotalMilliseconds < MAX_ACTIVATING_MS)
                        controller.Activate();
                    else break;
                }
            }

            timer.Stop();
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

                    HashSet<ulong> EntitiesWithInactive = new HashSet<ulong>(); // contains inactive entities too (not inactive players)

                    foreach (ulong uid in EntityControllers.Keys)
                    {
                        // -- checking entities to add --
                        EntityAbstraction entity = EntityControllers[uid];
                        Vector2 entityPos = entity.EntityData.Position;
                        bool isPlayer = entity.EntityData.ID == EntityID.Player;

                        const float MAX_DISTANCE = 50f;
                        if (Vector2.Distance(playerPos, entityPos) < MAX_DISTANCE)
                        {
                            if(entity.IsActive)
                            {
                                EntityList.Add(uid, entity.EntityData);
                                EntitiesWithInactive.Add(uid);

                                // -- adding indexes --
                                if (isPlayer)
                                {
                                    PlayerFixedIndexes.Add(uid, FixedFrames[uid]);
                                }
                            }
                            else
                            {
                                if (!isPlayer)
                                {
                                    EntitiesWithInactive.Add(uid);
                                }
                            }
                        }
                    }

                    References.PlayerManager.UpdateNearbyUIDs(
                        nickname,
                        EntitiesWithInactive,
                        FixedCounter,
                        UpdateCounter % 6 == 0
                        );

                    const int MAX_RECORDS = EntityBroadcast.MAX_RECORDS;
                    List<ulong> sendUIDs = EntityList.Keys.ToList();
                    for (int pos = 0; true; pos += MAX_RECORDS)
                    {
                        int sizeEnt = System.Math.Clamp(sendUIDs.Count - pos, 0, MAX_RECORDS);
                        if (sizeEnt == 0) break;

                        HashSet<ulong> fragmentedUIDs = sendUIDs.GetRange(pos, sizeEnt).ToHashSet();

                        Dictionary<ulong, EntityData> fragmentEntities = new();
                        foreach (ulong uid in fragmentedUIDs)
                        {
                            if (EntityList.TryGetValue(uid, out EntityData data))
                                fragmentEntities[uid] = data;
                        }

                        Dictionary<ulong, uint> fragmentFixed = new();
                        foreach (ulong uid in fragmentedUIDs)
                        {
                            if (PlayerFixedIndexes.TryGetValue(uid, out uint fixedIndex))
                                fragmentFixed[uid] = fixedIndex;
                        }

                        EntityBroadcast entityBroadcast = new EntityBroadcast(
                            FixedCounter,
                            fragmentEntities,
                            fragmentFixed
                            );
                        if (!entityBroadcast.HasProblems)
                        {
                            Packet packet = entityBroadcast.GetPacket();
                            References.Server.Send(nickname, packet, false); // unsafe mode (over raw UDP)
                        }
                        else throw new System.Exception("Couldn't construct EntityBroadcast message!");
                    }
                }

                UpdateCounter++;
                LastSentFixedCounter = FixedCounter;
            }
        }

        public void CreatePlayerController(string nickname)
        {
            EntityAbstraction playerController = new(nickname);
            PlayerControllers.Add(nickname, playerController);
            EntityControllers.Add(playerController.uID, playerController);

            // Construct and send PlayerInitialize
            PlayerInitialize answer = new PlayerInitialize(
                playerController.EntityData.Position,
                playerController.uID,
                LastSentFixedCounter
            );
            if (!answer.HasProblems)
            {
                References.Server.Send(nickname, answer.GetPacket());
            }
        }

        public EntityAbstraction GetPlayerController(string nickname)
        {
            if(PlayerControllers.ContainsKey(nickname))
                return PlayerControllers[nickname];
            return null;
        }

        public void UnloadPlayerController(string nickname)
        {
            EntityAbstraction playerController = PlayerControllers[nickname];
            PlayerControllers.Remove(nickname);
            EntityControllers.Remove(playerController.uID);
            playerController.UnloadEntityInstant();
        }

        public List<EntityAbstraction> GetAllPlayerControllers()
        {
            return PlayerControllers.Values.ToList();
        }

        public List<EntityAbstraction> GetAllEntityControllers()
        {
            return EntityControllers.Values.ToList();
        }

        public void SummonEntity(EntityData entityData)
        {
            EntityAbstraction entityController = new(entityData);
            EntityControllers.Add(entityController.uID, entityController);
        }

        public void LoadEntitiesByChunk(Vector2Int chunkCoords)
        {
            Dictionary<ulong, EntityData> entities = References.EntityDataManager.GetUnloadedEntitiesByChunk(chunkCoords);
            foreach (var kvp in entities)
            {
                EntityAbstraction entityController = new(kvp.Value, kvp.Key);
                EntityControllers.Add(entityController.uID, entityController);
            }
        }

        public void KillEntity(ulong uid)
        {
            if (!EntityControllers.ContainsKey(uid))
                throw new System.InvalidOperationException("Entity with ID " + uid + " is not loaded!");

            if (EntityControllers[uid].EntityData.ID == EntityID.Player)
            {
                foreach(string nickname in PlayerControllers.Keys.ToList())
                {
                    if (PlayerControllers[nickname].uID == uid)
                    {
                        PlayerControllers.Remove(nickname);

                        CodeInfo codeInfo = new CodeInfo((byte)CodeInfo.Info.YouDie);
                        if(!codeInfo.HasProblems)
                        {
                            References.Server.Send(nickname, codeInfo.GetPacket());
                        }

                        break;
                    }
                }
            }

            EntityControllers[uid].DeleteEntityInstant();
            EntityControllers.Remove(uid);
        }

        private void UnloadEntity(ulong uid)
        {
            if (!EntityControllers.ContainsKey(uid))
                throw new System.InvalidOperationException("Entity with ID " + uid + " is not loaded!");

            EntityAbstraction entityController = EntityControllers[uid];
            if (entityController.EntityData.ID == EntityID.Player)
                throw new System.InvalidOperationException("Cannot unload player this way!");

            EntityControllers[uid].UnloadEntityInstant();
            EntityControllers.Remove(uid);
        }
    }
}
