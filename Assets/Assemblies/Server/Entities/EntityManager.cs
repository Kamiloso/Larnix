using System.Collections;
using System.Collections.Generic;
using Larnix.Entities;
using System.Linq;
using Larnix.Socket.Packets;
using System.Diagnostics;
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
    internal class EntityManager : IScript
    {
        private readonly Dictionary<string, EntityAbstraction> _playerControllers = new();
        private readonly Dictionary<ulong, EntityAbstraction> _entityControllers = new();

        private Server Server => GlobRef.Get<Server>();
        private PlayerManager PlayerManager => GlobRef.Get<PlayerManager>();
        private QuickServer QuickServer => GlobRef.Get<QuickServer>();
        private EntityDataManager EntityDataManager => GlobRef.Get<EntityDataManager>();
        private Database Database => GlobRef.Get<Database>();
        private Chunks Chunks => GlobRef.Get<Chunks>();

        private uint _lastFixedFrame = 0;
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

        public void SendEntityBroadcast()
        {
            if (Server.FixedFrame != _lastFixedFrame)
            {
                List<(string Nickname, EntityBroadcast Packet)> broadcastsToSend = new();

                foreach (string nickname in PlayerManager.AllPlayers())
                {
                    ulong playerUID = PlayerManager.UidByNickname(nickname);
                    Vec2 playerPos = PlayerManager.RenderingPosition(nickname);

                    Dictionary<ulong, EntityData> entityList = new();
                    Dictionary<ulong, uint> playerFixedIndexes = new();

                    HashSet<ulong> entitiesWithInactive = new(); // contains inactive entities too (not inactive players)

                    foreach (ulong uid in _entityControllers.Keys)
                    {
                        if (uid == playerUID)
                            continue; // skip self

                        // -- checking entities to add --
                        EntityAbstraction entity = _entityControllers[uid];
                        Vec2 entityPos = entity.ActiveData.Position;

                        const float MAX_DISTANCE = 50f;
                        if (Vec2.Distance(playerPos, entityPos) < MAX_DISTANCE)
                        {
                            if (entity.IsActive)
                            {
                                entityList.Add(uid, entity.ActiveData);
                                entitiesWithInactive.Add(uid);

                                // -- adding indexes --
                                if (entity.IsPlayer)
                                {
                                    PlayerUpdate recentUpdate = PlayerManager.RecentPlayerUpdate(entity.Nickname);
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

                    PlayerManager.UpdateNearbyUIDs(
                        nickname,
                        entitiesWithInactive,
                        Server.FixedFrame,
                        _updateCounter % 6 == 0
                        );

                    var fragments = EntityBroadcast.CreateList(Server.FixedFrame, entityList, playerFixedIndexes);
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
                _lastFixedFrame = Server.FixedFrame;
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
                lastFixedFrame: _lastFixedFrame
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

            ulong uid = GetNextUID(); // generate new

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

        // ----- UID Management -----

        private ulong? _nextUID = null;
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
                _nextUID = (ulong)(Database.GetMinUID() - 1);
                return GetNextUID();
            }
        }
    }
}
