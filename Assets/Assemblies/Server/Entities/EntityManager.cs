using System.Collections;
using System.Collections.Generic;
using Larnix.Entities;
using System.Linq;
using Larnix.Socket.Packets;
using Larnix.Server.Terrain;
using Larnix.Core.Vectors;
using System;
using Larnix.Core.Utils;
using Larnix.Entities.Structs;
using Larnix.Socket.Backend;
using Larnix.Server.Data;
using Larnix.Packets;
using Larnix.Core.Json;
using Larnix.Core;

namespace Larnix.Server.Entities
{
    internal enum EntityLoadState { Loading, Active, Unloaded }
    internal class EntityManager : IScript
    {
        private readonly Dictionary<string, EntityAbstraction> _playerControllers = new();
        private readonly Dictionary<ulong, EntityAbstraction> _entityControllers = new();

        private Clock Clock => GlobRef.Get<Clock>();
        private Config Config => GlobRef.Get<Config>();
        private DataSaver DataSaver => GlobRef.Get<DataSaver>();
        private PlayerActions PlayerActions => GlobRef.Get<PlayerActions>();
        private QuickServer QuickServer => GlobRef.Get<QuickServer>();
        private EntityDataManager EntityDataManager => GlobRef.Get<EntityDataManager>();
        private Database Database => GlobRef.Get<Database>();
        private Chunks Chunks => GlobRef.Get<Chunks>();

        private uint _updateCounter = 0; // just to check modulo when sending NearbyEntities packet

        void IScript.EarlyFrameUpdate()
        {
            // MUST EXECUTE AFTER Chunks.EarlyFrameUpdate() TO
            // UNLOAD ENTITIES INSTANTLY AFTER CHUNK UNLOADING!!!

            // Unload entities that are in unloaded chunks
            foreach (ulong uid in _entityControllers.Keys.ToList())
            {
                EntityAbstraction controller = _entityControllers[uid];
                if (controller.IsActive && !controller.IsPlayer)
                {
                    if (Chunks.IsEntityInZone(controller, ChunkLoadState.None))
                        UnloadEntity(uid);
                }
            }

            // Activate entities that are in loaded chunks, but not active yet
            Common.DoForSeconds(3.0, (timer, seconds) =>
            {
                foreach (ulong uid in _entityControllers.Keys.ToList())
                {
                    EntityAbstraction controller = _entityControllers[uid];
                    if (!controller.IsActive && !controller.IsPlayer)
                    {
                        if (Chunks.IsEntityInZone(controller, ChunkLoadState.Active))
                        {
                            controller.Activate();
                            
                            double elapsed = timer.Elapsed.TotalSeconds;
                            if (elapsed >= seconds) return;
                        }
                    }
                }
            });
        }

        void IScript.FrameUpdate()
        {
            // Frame update for active entities
            foreach (var controller in _entityControllers.Values.ToList())
            {
                if (controller.IsActive)
                    controller.FrameUpdate();
            }

            // Kill entities when needed
            foreach (var controller in _entityControllers.Values.ToList())
            {
                if (controller.IsActive)
                {
                    Storage storage = controller.ActiveData.Data;
                    if (Tags.TryConsume(storage, "tags", Tags.TO_BE_KILLED))
                    {
                        ulong uid = controller.UID;
                        KillEntity(uid);
                    }
                }
            }
        }

        void IScript.PostLateFrameUpdate()
        {
            if (Clock.FixedFrame % Config.EntityBroadcastPeriodFrames == 0)
            {
                var broadcastsToSend = new List<(string Nickname, EntityBroadcast Packet)>();

                foreach (string nickname in PlayerActions.AllPlayers())
                {
                    ulong playerUID = PlayerActions.UidByNickname(nickname);
                    Vec2 playerPos = PlayerActions.RenderingPosition(nickname);

                    var entityList = new Dictionary<ulong, EntityData>();
                    var playerFixedIndexes = new Dictionary<ulong, uint>();
                    var entitiesWithInactive = new HashSet<ulong>(); // contains inactive entities too (not inactive players)

                    foreach (ulong uid in _entityControllers.Keys)
                    {
                        if (uid == playerUID)
                            continue; // skip self

                        // checking entities to add
                        EntityAbstraction entity = _entityControllers[uid];
                        Vec2 entityPos = entity.ActiveData.Position;

                        const float MAX_DISTANCE = 50f;
                        if (Vec2.Distance(playerPos, entityPos) < MAX_DISTANCE)
                        {
                            if (entity.IsActive)
                            {
                                entityList.Add(uid, entity.ActiveData);
                                entitiesWithInactive.Add(uid);

                                // adding indexes
                                if (entity.IsPlayer)
                                {
                                    PlayerUpdate recentUpdate = PlayerActions.RecentPlayerUpdate(entity.Nickname);
                                    uint fixedFrame = recentUpdate.FixedFrame;

                                    playerFixedIndexes.Add(uid, fixedFrame);
                                }
                            }
                            else
                            {
                                if (!entity.IsPlayer)
                                {
                                    entitiesWithInactive.Add(uid);
                                }
                            }
                        }
                    }

                    bool sendAtLeastOne = _updateCounter % 6 == 0;
                    PlayerActions.UpdateNearbyUIDs(
                        nickname, entitiesWithInactive, Clock.FixedFrame, sendAtLeastOne);

                    var fragments = EntityBroadcast.CreateList(Clock.FixedFrame, entityList, playerFixedIndexes);
                    foreach (var brdcst in fragments)
                    {
                        broadcastsToSend.Add((nickname, brdcst));
                    }
                }

                broadcastsToSend = broadcastsToSend.OrderBy(x => Common.Rand().Next()).ToList();
                foreach (var pair in broadcastsToSend)
                {
                    QuickServer.Send(pair.Nickname, pair.Packet, false); // fast mode (over raw UDP)
                }

                _updateCounter++;
            }
        }

        public void CreatePlayerController(string nickname)
        {
            EntityAbstraction controller = EntityAbstraction.CreatePlayerController(nickname);
            _playerControllers.Add(nickname, controller);
            _entityControllers.Add(controller.UID, controller);

            Payload packet = new PlayerInitialize(
                position: controller.ActiveData.Position,
                myUid: controller.UID,
                lastFixedFrame: Clock.FixedFrame - 1
            );
            QuickServer.Send(nickname, packet);
        }

        public EntityAbstraction GetPlayerController(string nickname)
        {
            if (_playerControllers.TryGetValue(nickname, out var value))
                return value;
            
            return null;
        }

        public void UnloadPlayerController(string nickname)
        {
            if (_playerControllers.TryGetValue(nickname, out var value))
            {
                EntityAbstraction controller = value;
                _playerControllers.Remove(nickname);
                _entityControllers.Remove(controller.UID);
                controller.UnloadEntityInstant();
            }
            else throw new InvalidOperationException($"Controller of player {nickname} is not loaded!");
        }

        public bool SummonEntity(EntityData entityData)
        {
            if (!Chunks.IsLoadedPosition(entityData.Position))
                return false;

            ulong uid = EntityDataManager.NextUID(); // generate new

            EntityAbstraction controller = EntityAbstraction.CreateEntityController(entityData, uid);
            _entityControllers.Add(controller.UID, controller);
            return true;
        }

        public void PrepareEntitiesByChunk(Vec2Int chunkCoords)
        {
            Dictionary<ulong, EntityData> entities = EntityDataManager.GetUnloadedEntitiesByChunk(chunkCoords);
            foreach (var kvp in entities)
            {
                EntityAbstraction controller = EntityAbstraction.CreateEntityController(kvp.Value, kvp.Key);
                _entityControllers.Add(controller.UID, controller);
            }
        }

        public void KillEntity(ulong uid)
        {
            if (_entityControllers.TryGetValue(uid, out var controller))
            {
                if (controller.IsPlayer)
                {
                    string nickname = controller.Nickname;
                    _playerControllers.Remove(nickname);

                    CodeInfo packet = new CodeInfo(CodeInfo.Info.YouDie);
                    QuickServer.Send(nickname, packet);
                }

                controller.DeleteEntityInstant();
                _entityControllers.Remove(uid);
            }
            else throw new InvalidOperationException("Entity with ID " + uid + " is not loaded!");
        }

        private void UnloadEntity(ulong uid)
        {
            if (_entityControllers.TryGetValue(uid, out var controller))
            {
                if (controller.IsPlayer)
                {
                    throw new InvalidOperationException("Trying to unload a player entity!");
                }

                controller.UnloadEntityInstant();
                _entityControllers.Remove(uid);
            }
            else throw new InvalidOperationException("Entity with ID " + uid + " is not loaded!");
        }
    }
}
