#nullable enable
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Model.Entities.Structs;
using Larnix.Model.Enums;
using Larnix.Model.Json;
using Larnix.Model.Utils;
using Larnix.Server.Data;
using Larnix.Server.Entities.Controllers;
using Larnix.Server.Packets;
using Larnix.Server.Terrain;
using Larnix.Model.Worldgen;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Server.Entities;

internal enum EntityLoadState
{
    Loading,
    Active,
    Unloaded
}

internal interface IEntityManager : IScript { }

internal class EntityManager : IEntityManager
{
    private IServer Server => GlobRef.Get<IServer>();
    private IClock Clock => GlobRef.Get<IClock>();
    private IEntityControllers EntityControllers => GlobRef.Get<IEntityControllers>();
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();
    private Generator Generator => GlobRef.Get<Generator>();
    private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();
    private Chunks Chunks => GlobRef.Get<Chunks>();

    void IScript.EarlyFrameUpdate()
    {
        // MUST EXECUTE AFTER Chunks.EarlyFrameUpdate() TO
        // UNLOAD ENTITIES INSTANTLY AFTER CHUNK UNLOADING!!!

        // Unload entities that are in unloaded chunks
        foreach (ulong uid in EntityControllers.EntityUids)
        {
            EntityController controller = EntityControllers.GetEntityController(uid)!;
            if (Chunks.IsUnloadedPosition(controller.Position))
            {
                EntityControllers.UnloadController(uid);
            }
        }

        // Activate entities that are in loaded chunks, but not active yet
        Common.DoForSeconds(3.0, (timer, seconds) =>
        {
            foreach (ulong uid in EntityControllers.EntityUids)
            {
                EntityController controller = EntityControllers.GetEntityController(uid)!;
                if (!controller.IsActive)
                {
                    if (Chunks.IsLoadedPosition(controller.Position))
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
        foreach (ulong uid in EntityControllers.Uids)
        {
            BaseController controller = EntityControllers.GetController(uid)!;
            if (controller.IsActive)
            {
                controller.FrameUpdate();
            }
        }

        // Kill entities when needed
        foreach (ulong uid in EntityControllers.Uids)
        {
            BaseController controller = EntityControllers.GetController(uid)!;
            if (controller.IsActive)
            {
                Storage storage = controller.ActiveData.NBT;
                if (Tags.TryConsume(storage, "tags", Tags.TO_BE_KILLED))
                {
                    EntityControllers.KillController(uid);
                }
            }
        }
    }

    void IScript.PostLateFrameUpdate()
    {
        if (Clock.FixedFrame % ServerConfig.PeriodicTasks_EntityBroadcastPeriodFrames == 0)
        {
            var broadcastsToSend = new List<(string Nickname, EntityBroadcast Packet)>();

            foreach (string nickname in ConnectedPlayers.AllPlayers) // ITER: every connected player
            {
                ulong playerUID = ConnectedPlayers.UidByNickname(nickname);
                Vec2 playerPos = ConnectedPlayers[nickname].RenderPosition;

                var entityHeaders = new Dictionary<ulong, EntityHeader>();
                var playerFixedIndexes = new Dictionary<ulong, uint>();
                var nearbyUids = new HashSet<ulong>(); // inactive entities too (no inactive players)

                foreach (ulong uid in EntityControllers.Uids) // ITER: every entity controller
                {
                    if (uid == playerUID) continue; // skip self

                    // checking entities to add
                    BaseController controller = EntityControllers.GetController(uid)!;
                    Vec2 entityPos = controller.Position;

                    if (Vec2.Distance(playerPos, entityPos) < Common.ViewDistance)
                    {
                        if (controller.IsActive)
                        {
                            entityHeaders.Add(uid, controller.ActiveData.Header);
                            nearbyUids.Add(uid);

                            // adding indexes
                            if (controller is PlayerController playerController)
                            {
                                string entityNickname = playerController.Nickname;
                                PlayerUpdate recentUpdate = ConnectedPlayers[entityNickname].LastUpdate!;
                                uint fixedFrame = recentUpdate.FixedFrame;

                                playerFixedIndexes.Add(uid, fixedFrame);
                            }
                        }
                        else
                        {
                            if (controller is not PlayerController)
                            {
                                nearbyUids.Add(uid);
                            }
                        }
                    }
                }

                bool sendAtLeastOne = Clock.FixedFrame % 6 == 0;
                SendNearbyUids(nickname, ConnectedPlayers[nickname].NearbyEntityUids, nearbyUids, sendAtLeastOne);
                ConnectedPlayers[nickname].NearbyEntityUids = nearbyUids;

                List<EntityBroadcast> fragments = EntityBroadcast.CreateList(Clock.FixedFrame, entityHeaders, playerFixedIndexes);
                foreach (var brdcst in fragments)
                {
                    broadcastsToSend.Add((nickname, brdcst));
                }

                FrameInfo framePacket = new(
                    serverTick: Clock.ServerTick,
                    skyColor: Generator.SkyColorAt(playerPos),
                    biomeID: Generator.BiomeAt(playerPos),
                    weather: WeatherID.Clear,
                    tps: Clock.TPS
                );

                Server.SendFast(nickname, framePacket);
            }

            broadcastsToSend = broadcastsToSend
                .OrderBy(_ => RandUtils.NextInt())
                .ToList();

            foreach (var (nickname, packet) in broadcastsToSend)
            {
                Server.SendFast(nickname, packet);
            }
        }
    }

    private void SendNearbyUids(string nickname, HashSet<ulong> oldUids, HashSet<ulong> newUids, bool sendAtLeastOne)
    {
        ulong[] toAdd = newUids.Except(oldUids).ToArray();
        ulong[] toRemove = oldUids.Except(newUids).ToArray();

        uint fixedFrame = Clock.FixedFrame;
        List<NearbyEntities> packets = NearbyEntities.CreateList(fixedFrame, toAdd, toRemove);

        if (sendAtLeastOne && packets.Count == 0)
        {
            packets.Add(NearbyEntities.CreateBootstrap(fixedFrame));
        }

        foreach (NearbyEntities packet in packets)
        {
            Server.Send(nickname, packet);
        }
    }
}
