using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using System.Linq;
using Larnix.Packets;
using System.Diagnostics;
using Larnix.Server.Terrain;
using QuickNet.Channel;
using System;

namespace Larnix.Server.Entities
{
    public class EntityManager : MonoBehaviour
    {
        private readonly Dictionary<string, EntityAbstraction> PlayerControllers = new();
        private readonly Dictionary<ulong, EntityAbstraction> EntityControllers = new();
        public int EntityCount = 0;

        private uint FixedCounter = 0;
        private uint LastSentFixedCounter = 0;
        private uint UpdateCounter = 0; // just to check modulo when sending NearbyEntities packet

        private void Awake()
        {
            References.EntityManager = this;
        }

        public void FromFixedUpdate() // FIX-2
        {
            // Fixed counter increment
            FixedCounter++;

            // Execute entity behaviours

            // DON'T add .ToList() or anything like that here.
            // We need a clear error message when accessing this dictionary inproperly.
            foreach (var controller in EntityControllers.Values)
                if (controller.IsActive)
                {
                    controller.FromFixedUpdate();
                }

            // Kill entities when needed
            foreach (ulong uid in EntityControllers.Keys.ToList())
                if (EntityControllers[uid].IsActive)
                {
                    if (EntityControllers[uid].EntityData.NBT == "something... idk")
                        KillEntity(uid);
                }

            // Debug info
            EntityCount = EntityControllers.Count;
        }

        public void FromEarlyUpdate() // 2
        {
            // Unload entities
            foreach(ulong uid in EntityControllers.Keys.ToList())
            {
                EntityAbstraction controller = EntityControllers[uid];
                if (controller.IsActive && controller.EntityData.ID != EntityID.Player)
                {
                    if (References.ChunkLoading.IsEntityInZone(controller, ChunkLoading.LoadState.None))
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
                    if(References.ChunkLoading.IsEntityInZone(controller, ChunkLoading.LoadState.Active))
                    {
                        if (timer.Elapsed.TotalMilliseconds < MAX_ACTIVATING_MS)
                            controller.Activate();
                        else break;
                    }
                }
            }

            timer.Stop();
        }

        public void SendEntityBroadcast() // It works, better don't touch it
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
                        int sizeEnt = Math.Clamp(sendUIDs.Count - pos, 0, MAX_RECORDS);
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

                        Packet packet = new EntityBroadcast(FixedCounter, fragmentEntities, fragmentFixed);
                        References.Server.Send(nickname, packet, false); // unsafe mode (over raw UDP)
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
            Packet packet = new PlayerInitialize(
                playerController.EntityData.Position,
                playerController.uID,
                LastSentFixedCounter
            );
            References.Server.Send(nickname, packet);
        }

        public EntityAbstraction GetPlayerController(string nickname)
        {
            PlayerControllers.TryGetValue(nickname, out var value);
            return value;
        }

        public void UnloadPlayerController(string nickname)
        {
            if (PlayerControllers.TryGetValue(nickname, out var value))
            {
                EntityAbstraction playerController = value;
                PlayerControllers.Remove(nickname);
                EntityControllers.Remove(playerController.uID);
                playerController.UnloadEntityInstant();
            }
            else throw new InvalidOperationException($"Controller of player {nickname} is not loaded!");
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

        public bool IsEntityLoaded(ulong uid)
        {
            return EntityControllers.ContainsKey(uid);
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

                        CodeInfo packet = new CodeInfo(CodeInfo.Info.YouDie);
                        References.Server.Send(nickname, packet);

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
                throw new InvalidOperationException("Entity with ID " + uid + " is not loaded!");

            EntityAbstraction entityController = EntityControllers[uid];
            if (entityController.EntityData.ID == EntityID.Player)
                throw new InvalidOperationException("Cannot unload player this way!");

            EntityControllers[uid].UnloadEntityInstant();
            EntityControllers.Remove(uid);
        }
    }
}
