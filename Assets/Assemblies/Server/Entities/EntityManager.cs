using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Larnix.Socket.Packets;
using Larnix.Server.Terrain;
using Larnix.Core.Vectors;
using System;
using Larnix.Model.Utils;
using Larnix.Core;
using Larnix.Model.Entities.Structs;
using Larnix.Socket.Backend;
using Larnix.Server.Data;
using Larnix.Server.Packets;
using Larnix.Model.Json;
using Larnix.Core.Utils;

namespace Larnix.Server.Entities;

internal enum EntityLoadState
{
    Loading,
    Active,
    Unloaded
}

internal interface IEntityManager : IScript
{
    void CreatePlayerController(string nickname);
    bool TryGetPlayerController(string nickname, out EntityAbstraction controller);
    bool TryUnloadPlayerController(string nickname);
    bool SummonEntity(EntityData entityData);
    void PrepareEntitiesByChunk(Vec2Int chunkCoords);
    void KillEntity(ulong uid);
}

internal class EntityManager : IEntityManager
{
    private readonly Dictionary<string, EntityAbstraction> _playerControllers = new();
    private readonly Dictionary<ulong, EntityAbstraction> _entityControllers = new();

    private IClock Clock => GlobRef.Get<IClock>();
    private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();
    private IPlayerActions PlayerActions => GlobRef.Get<IPlayerActions>();
    private QuickServer QuickServer => GlobRef.Get<QuickServer>();
    private EntityDataManager EntityDataManager => GlobRef.Get<EntityDataManager>();
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
            if (!controller.IsPlayer)
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
                Storage storage = controller.ActiveData.NBT;
                if (Tags.TryConsume(storage, "tags", Tags.TO_BE_KILLED))
                {
                    ulong uid = controller.Uid;
                    KillEntity(uid);
                }
            }
        }
    }

    void IScript.PostLateFrameUpdate()
    {
        if (Clock.FixedFrame % ServerConfig.PeriodicTasks_EntityBroadcastPeriodFrames == 0)
        {
            var broadcastsToSend = new List<(string Nickname, EntityBroadcast Packet)>();

            foreach (string nickname in PlayerActions.AllPlayers())
            {
                ulong playerUID = PlayerActions.UidByNickname(nickname);
                Vec2 playerPos = PlayerActions.RenderingPosition(nickname);

                var entityHeaders = new Dictionary<ulong, EntityHeader>();
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
                            entityHeaders.Add(uid, entity.ActiveData.Header);
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

                var fragments = EntityBroadcast.CreateList(Clock.FixedFrame, entityHeaders, playerFixedIndexes);
                foreach (var brdcst in fragments)
                {
                    broadcastsToSend.Add((nickname, brdcst));
                }
            }

            broadcastsToSend = broadcastsToSend
                .OrderBy(_ => RandUtils.NextInt())
                .ToList();

            foreach (var pair in broadcastsToSend)
            {
                string nickname = pair.Nickname;
                Payload packet = pair.Packet;

                QuickServer.Send(nickname, packet, false); // fast mode (over raw UDP)
            }

            _updateCounter++;
        }
    }

    public void CreatePlayerController(string nickname)
    {
        EntityAbstraction controller = EntityAbstraction.CreatePlayerController(nickname);
        _playerControllers.Add(nickname, controller);
        _entityControllers.Add(controller.Uid, controller);

        Payload packet = new PlayerInitialize(
            position: controller.ActiveData.Position,
            myUid: controller.Uid,
            lastFixedFrame: Clock.FixedFrame - 1
        );

        QuickServer.Send(nickname, packet);
    }

    public bool TryGetPlayerController(string nickname, out EntityAbstraction controller)
    {
        return _playerControllers.TryGetValue(nickname, out controller);
    }

    public bool TryUnloadPlayerController(string nickname)
    {
        if (!_playerControllers.TryGetValue(nickname, out var controller))
            return false;

        _playerControllers.Remove(nickname);
        _entityControllers.Remove(controller.Uid);
        controller.UnloadEntityInstant();

        return true;
    }

    public bool SummonEntity(EntityData entityData)
    {
        if (!Chunks.IsLoadedPosition(entityData.Position))
            return false;

        ulong uid = EntityDataManager.NextUID(); // generate new

        EntityAbstraction controller = EntityAbstraction.CreateEntityController(entityData, uid);
        _entityControllers.Add(controller.Uid, controller);

        return true;
    }

    public void PrepareEntitiesByChunk(Vec2Int chunkCoords)
    {
        Dictionary<ulong, EntityData> entitiesToActivate = EntityDataManager.GetUnloadedEntitiesByChunk(chunkCoords);
        foreach (var kvp in entitiesToActivate)
        {
            ulong uid = kvp.Key;
            EntityData entityData = kvp.Value;

            EntityAbstraction controller = EntityAbstraction.CreateEntityController(entityData, uid);
            _entityControllers.Add(controller.Uid, controller);
        }
    }

    public void KillEntity(ulong uid)
    {
        if (!_entityControllers.TryGetValue(uid, out var controller))
            throw new InvalidOperationException("Entity with ID = " + uid + " is not loaded!");
        
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

    private void UnloadEntity(ulong uid)
    {
        if (!_entityControllers.TryGetValue(uid, out var controller))
            throw new InvalidOperationException("Entity with ID = " + uid + " is not loaded!");

        if (controller.IsPlayer)
            throw new InvalidOperationException("Trying to unload a player entity!");

        controller.UnloadEntityInstant();
        _entityControllers.Remove(uid);
    }
}
