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
using Larnix.Core.References;
using Larnix.Packets;
using Larnix.Core.Json;
using Larnix.Core;

namespace Larnix.Server.Entities
{
    internal class EntityManager : Singleton
    {
        private readonly Dictionary<string, EntityAbstraction> _playerControllers = new();
        private readonly Dictionary<ulong, EntityAbstraction> _entityControllers = new();

        private Server Server => Ref<Server>();
        private PlayerManager PlayerManager => Ref<PlayerManager>();
        private QuickServer QuickServer => Ref<QuickServer>();
        private EntityDataManager EntityDataManager => Ref<EntityDataManager>();
        private Database Database => Ref<Database>();
        private ChunkLoading ChunkLoading => Ref<ChunkLoading>();

        private uint _lastFixedFrame = 0;
        private uint _updateCounter = 0; // just to check modulo when sending NearbyEntities packet
        private ulong? _nextUID = null;

        public EntityManager(Server server) : base(server) { }

        public override void FrameUpdate()
        {
            // DON'T add .ToList() or anything like that here.
            // We need a clear error message when accessing this dictionary inproperly.
            foreach (var controller in _entityControllers.Values)
                if (controller.IsActive)
                {
                    controller.FromFrameUpdate();
                }

            // Kill entities when needed
            foreach (ulong uid in _entityControllers.Keys.ToList())
                if (_entityControllers[uid].IsActive)
                {
                    Storage storage = _entityControllers[uid].EntityData.Data;
                    if (Tags.TryConsume(storage, "tags", Tags.TO_BE_KILLED))
                    {
                        KillEntity(uid);
                    }
                }
        }

        public override void EarlyFrameUpdate()
        {
            // Unload entities
            foreach(ulong uid in _entityControllers.Keys.ToList())
            {
                EntityAbstraction controller = _entityControllers[uid];
                if (controller.IsActive && controller.EntityData.ID != EntityID.Player)
                {
                    if (ChunkLoading.IsEntityInZone(controller, ChunkLoading.LoadState.None))
                        UnloadEntity(uid);
                }
            }

            // Activate entities
            const double MAX_ACTIVATING_MS = 3.0f;
            Stopwatch timer = Stopwatch.StartNew();

            foreach (ulong uid in _entityControllers.Keys.ToList())
            {
                EntityAbstraction controller = _entityControllers[uid];
                if (!controller.IsActive && controller.EntityData.ID != EntityID.Player)
                {
                    if(ChunkLoading.IsEntityInZone(controller, ChunkLoading.LoadState.Active))
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
            if(Server.FixedFrame != _lastFixedFrame)
            {
                Dictionary<ulong, uint> FixedFrames = PlayerManager.GetFixedFramesByUID();
                List<(string, EntityBroadcast)> broadcastsToSend = new();

                foreach (string nickname in PlayerManager.AllPlayers())
                {
                    ulong playerUID = PlayerManager.GetPlayerUID(nickname);
                    Vec2 playerPos = PlayerManager.GetPlayerRenderingPosition(nickname);

                    Dictionary<ulong, EntityData> EntityList = new();
                    Dictionary<ulong, uint> PlayerFixedIndexes = new();

                    HashSet<ulong> EntitiesWithInactive = new(); // contains inactive entities too (not inactive players)

                    foreach (ulong uid in _entityControllers.Keys)
                    {
                        if (uid == playerUID)
                            continue; // skip self

                        // -- checking entities to add --
                        EntityAbstraction entity = _entityControllers[uid];
                        Vec2 entityPos = entity.EntityData.Position;
                        bool isPlayer = entity.EntityData.ID == EntityID.Player;

                        const float MAX_DISTANCE = 50f;
                        if (Vec2.Distance(playerPos, entityPos) < MAX_DISTANCE)
                        {
                            if (entity.IsActive)
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

                    PlayerManager.UpdateNearbyUIDs(
                        nickname,
                        EntitiesWithInactive,
                        Server.FixedFrame,
                        _updateCounter % 6 == 0
                        );

                    var fragments = EntityBroadcast.CreateList(Server.FixedFrame, EntityList, PlayerFixedIndexes);
                    foreach (var brdcst in fragments)
                    {
                        broadcastsToSend.Add((nickname, brdcst));
                    }
                }

                broadcastsToSend = broadcastsToSend.OrderBy(x => Common.Rand().Next()).ToList();
                foreach (var pair in broadcastsToSend)
                {
                    QuickServer.Send(pair.Item1, pair.Item2, false); // unsafe mode (over raw UDP)
                }

                _updateCounter++;
                _lastFixedFrame = Server.FixedFrame;
            }
        }

        public void CreatePlayerController(string nickname)
        {
            EntityAbstraction playerController = EntityAbstraction.CreatePlayerController(this, nickname);
            _playerControllers.Add(nickname, playerController);
            _entityControllers.Add(playerController.uID, playerController);

            Payload packet = new PlayerInitialize(
                position: playerController.EntityData.Position,
                myUid: playerController.uID,
                lastFixedFrame: _lastFixedFrame
            );
            QuickServer.Send(nickname, packet);
        }

        public EntityAbstraction GetPlayerController(string nickname)
        {
            _playerControllers.TryGetValue(nickname, out var value);
            return value;
        }

        public void UnloadPlayerController(string nickname)
        {
            if (_playerControllers.TryGetValue(nickname, out var value))
            {
                EntityAbstraction playerController = value;
                _playerControllers.Remove(nickname);
                _entityControllers.Remove(playerController.uID);
                playerController.UnloadEntityInstant();
            }
            else throw new InvalidOperationException($"Controller of player {nickname} is not loaded!");
        }

        public List<EntityAbstraction> GetAllPlayerControllers() => _playerControllers.Values.ToList();
        public List<EntityAbstraction> GetAllEntityControllers() => _entityControllers.Values.ToList();

        public bool SummonEntity(EntityData entityData)
        {
            if (!ChunkLoading.IsLoadedPosition(entityData.Position))
                return false;

            EntityAbstraction entityController = EntityAbstraction.CreateEntityController(this, entityData);
            _entityControllers.Add(entityController.uID, entityController);
            return true;
        }

        public void LoadEntitiesByChunk(Vec2Int chunkCoords)
        {
            Dictionary<ulong, EntityData> entities = EntityDataManager.GetUnloadedEntitiesByChunk(chunkCoords);
            foreach (var kvp in entities)
            {
                EntityAbstraction entityController = EntityAbstraction.CreateEntityController(this, kvp.Value, kvp.Key);
                _entityControllers.Add(entityController.uID, entityController);
            }
        }

        public bool IsEntityLoaded(ulong uid)
        {
            return _entityControllers.ContainsKey(uid);
        }

        public void KillEntity(ulong uid)
        {
            if (!_entityControllers.ContainsKey(uid))
                throw new InvalidOperationException("Entity with ID " + uid + " is not loaded!");

            if (_entityControllers[uid].EntityData.ID == EntityID.Player)
            {
                foreach(string nickname in _playerControllers.Keys.ToList())
                {
                    if (_playerControllers[nickname].uID == uid)
                    {
                        _playerControllers.Remove(nickname);

                        CodeInfo packet = new CodeInfo(CodeInfo.Info.YouDie);
                        QuickServer.Send(nickname, packet);

                        break;
                    }
                }
            }

            _entityControllers[uid].DeleteEntityInstant();
            _entityControllers.Remove(uid);
        }

        private void UnloadEntity(ulong uid)
        {
            if (!_entityControllers.ContainsKey(uid))
                throw new InvalidOperationException("Entity with ID " + uid + " is not loaded!");

            EntityAbstraction entityController = _entityControllers[uid];
            if (entityController.EntityData.ID == EntityID.Player)
                throw new InvalidOperationException("Cannot unload player this way!");

            _entityControllers[uid].UnloadEntityInstant();
            _entityControllers.Remove(uid);
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
                _nextUID = (ulong)(Database.GetMinUID() - 1);
                return GetNextUID();
            }
        }
    }
}
