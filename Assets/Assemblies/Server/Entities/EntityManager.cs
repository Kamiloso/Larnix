using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using System.Linq;
using Larnix.Packets;
using System.Diagnostics;
using Larnix.Server.Terrain;
using Larnix.Core.Vectors;
using System;
using Larnix.Core.Utils;
using Larnix.Entities.Structs;
using Larnix.Socket.Backend;
using Larnix.Server.Data;
using Larnix.Server.References;
using Larnix.Packets.Game;

namespace Larnix.Server.Entities
{
    internal class EntityManager : ServerSingleton
    {
        private readonly Dictionary<string, EntityAbstraction> playerControllers = new();
        private readonly Dictionary<ulong, EntityAbstraction> entityControllers = new();

        private uint _lastFixedFrame = 0;
        private uint _updateCounter = 0; // just to check modulo when sending NearbyEntities packet
        private ulong? _nextUID = null;

        public EntityManager(Server server) : base(server) { }

        public override void FrameUpdate()
        {
            // DON'T add .ToList() or anything like that here.
            // We need a clear error message when accessing this dictionary inproperly.
            foreach (var controller in entityControllers.Values)
                if (controller.IsActive)
                {
                    controller.FromFrameUpdate();
                }

            // Kill entities when needed
            foreach (ulong uid in entityControllers.Keys.ToList())
                if (entityControllers[uid].IsActive)
                {
                    if (entityControllers[uid].EntityData.NBT.MarkedToKill)
                        KillEntity(uid);
                }
        }

        public override void EarlyFrameUpdate()
        {
            // Unload entities
            foreach(ulong uid in entityControllers.Keys.ToList())
            {
                EntityAbstraction controller = entityControllers[uid];
                if (controller.IsActive && controller.EntityData.ID != EntityID.Player)
                {
                    if (Ref<ChunkLoading>().IsEntityInZone(controller, ChunkLoading.LoadState.None))
                        UnloadEntity(uid);
                }
            }

            // Activate entities
            const double MAX_ACTIVATING_MS = 3.0f;
            Stopwatch timer = Stopwatch.StartNew();

            foreach (ulong uid in entityControllers.Keys.ToList())
            {
                EntityAbstraction controller = entityControllers[uid];
                if (!controller.IsActive && controller.EntityData.ID != EntityID.Player)
                {
                    if(Ref<ChunkLoading>().IsEntityInZone(controller, ChunkLoading.LoadState.Active))
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
            if(Ref<Server>().FixedFrame != _lastFixedFrame)
            {
                Dictionary<ulong, uint> FixedFrames = Ref<PlayerManager>().GetFixedFramesByUID();
                List<(string, EntityBroadcast)> broadcastsToSend = new();

                foreach (string nickname in Ref<PlayerManager>().GetAllPlayerNicknames())
                {
                    Vec2 playerPos = Ref<PlayerManager>().GetPlayerRenderingPosition(nickname);

                    Dictionary<ulong, EntityData> EntityList = new();
                    Dictionary<ulong, uint> PlayerFixedIndexes = new();

                    HashSet<ulong> EntitiesWithInactive = new HashSet<ulong>(); // contains inactive entities too (not inactive players)

                    foreach (ulong uid in entityControllers.Keys)
                    {
                        // -- checking entities to add --
                        EntityAbstraction entity = entityControllers[uid];
                        Vec2 entityPos = entity.EntityData.Position;
                        bool isPlayer = entity.EntityData.ID == EntityID.Player;

                        const float MAX_DISTANCE = 50f;
                        if ((playerPos - entityPos).Magnitude < MAX_DISTANCE)
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

                    Ref<PlayerManager>().UpdateNearbyUIDs(
                        nickname,
                        EntitiesWithInactive,
                        Ref<Server>().FixedFrame,
                        _updateCounter % 6 == 0
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

                        EntityBroadcast brdcst = new EntityBroadcast(Ref<Server>().FixedFrame, fragmentEntities, fragmentFixed);
                        broadcastsToSend.Add((nickname, brdcst));
                    }
                }

                broadcastsToSend = broadcastsToSend.OrderBy(x => Common.Rand().Next()).ToList();
                foreach (var pair in broadcastsToSend)
                {
                    Ref<QuickServer>().Send(pair.Item1, pair.Item2, false); // unsafe mode (over raw UDP)
                }

                _updateCounter++;
                _lastFixedFrame = Ref<Server>().FixedFrame;
            }
        }

        public void CreatePlayerController(string nickname)
        {
            EntityAbstraction playerController = new(this, nickname);
            playerControllers.Add(nickname, playerController);
            entityControllers.Add(playerController.uID, playerController);

            // Construct and send PlayerInitialize
            Payload packet = new PlayerInitialize(
                playerController.EntityData.Position,
                playerController.uID,
                _lastFixedFrame
            );
            Ref<QuickServer>().Send(nickname, packet);
        }

        public EntityAbstraction GetPlayerController(string nickname)
        {
            playerControllers.TryGetValue(nickname, out var value);
            return value;
        }

        public void UnloadPlayerController(string nickname)
        {
            if (playerControllers.TryGetValue(nickname, out var value))
            {
                EntityAbstraction playerController = value;
                playerControllers.Remove(nickname);
                entityControllers.Remove(playerController.uID);
                playerController.UnloadEntityInstant();
            }
            else throw new InvalidOperationException($"Controller of player {nickname} is not loaded!");
        }

        public List<EntityAbstraction> GetAllPlayerControllers()
        {
            return playerControllers.Values.ToList();
        }

        public List<EntityAbstraction> GetAllEntityControllers()
        {
            return entityControllers.Values.ToList();
        }

        public void SummonEntity(EntityData entityData)
        {
            EntityAbstraction entityController = new(this, entityData);
            entityControllers.Add(entityController.uID, entityController);
        }

        public void LoadEntitiesByChunk(Vector2Int chunkCoords)
        {
            Dictionary<ulong, EntityData> entities = Ref<EntityDataManager>().GetUnloadedEntitiesByChunk(chunkCoords);
            foreach (var kvp in entities)
            {
                EntityAbstraction entityController = new(this, kvp.Value, kvp.Key);
                entityControllers.Add(entityController.uID, entityController);
            }
        }

        public bool IsEntityLoaded(ulong uid)
        {
            return entityControllers.ContainsKey(uid);
        }

        public void KillEntity(ulong uid)
        {
            if (!entityControllers.ContainsKey(uid))
                throw new System.InvalidOperationException("Entity with ID " + uid + " is not loaded!");

            if (entityControllers[uid].EntityData.ID == EntityID.Player)
            {
                foreach(string nickname in playerControllers.Keys.ToList())
                {
                    if (playerControllers[nickname].uID == uid)
                    {
                        playerControllers.Remove(nickname);

                        CodeInfo packet = new CodeInfo(CodeInfo.Info.YouDie);
                        Ref<QuickServer>().Send(nickname, packet);

                        break;
                    }
                }
            }

            entityControllers[uid].DeleteEntityInstant();
            entityControllers.Remove(uid);
        }

        private void UnloadEntity(ulong uid)
        {
            if (!entityControllers.ContainsKey(uid))
                throw new InvalidOperationException("Entity with ID " + uid + " is not loaded!");

            EntityAbstraction entityController = entityControllers[uid];
            if (entityController.EntityData.ID == EntityID.Player)
                throw new InvalidOperationException("Cannot unload player this way!");

            entityControllers[uid].UnloadEntityInstant();
            entityControllers.Remove(uid);
        }

        public ulong GetNextUID()
        {
            if (_nextUID != null)
            {
                ulong uid = _nextUID.Value;
                _nextUID--;
                return uid;
            }
            else
            {
                _nextUID = (ulong)(Ref<Database>().GetMinUID() - 1);
                return GetNextUID();
            }
        }
    }
}
