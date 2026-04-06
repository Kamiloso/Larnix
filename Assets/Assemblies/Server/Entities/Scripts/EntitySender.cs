#nullable enable
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Model.Entities.Structs;
using Larnix.Model.Enums;
using Larnix.Model.Utils;
using Larnix.Model.Worldgen;
using Larnix.Server;
using Larnix.Server.Data;
using Larnix.Server.Entities;
using Larnix.Server.Entities.Controllers;
using Larnix.Server.Packets;
using System.Collections.Generic;
using System.Linq;

internal interface IEntitySender : IScript { }

internal class EntitySender : IEntitySender
{
    private IServer Server => GlobRef.Get<IServer>();
    private IClock Clock => GlobRef.Get<IClock>();
    private IEntityControllers EntityControllers => GlobRef.Get<IEntityControllers>();
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();
    private IGenerator Generator => GlobRef.Get<IGenerator>();
    private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();

    private record PlayerContext(
        Dictionary<ulong, EntityHeader> Headers,
        Dictionary<ulong, uint> PlayerFixedIndexes,
        HashSet<ulong> NearbyUids
    );

    void IScript.PostLateFrameUpdate()
    {
        if (Clock.FixedFrame % ServerConfig.PeriodicTasks_EntityBroadcastPeriodFrames == 0)
        {
            SendBroadcasts();
            SendFrameInfo();
        }
    }

    private void SendBroadcasts()
    {
        var result = new List<(string, EntityBroadcast)>();

        foreach (string nickname in ConnectedPlayers.AllPlayers)
        {
            var player = ConnectedPlayers[nickname];
            ulong playerUID = ConnectedPlayers.UidByNickname(nickname);
            Vec2 playerPos = player.RenderPosition;

            var context = BuildPlayerContext(playerUID, playerPos);

            SendNearbyDiff(nickname, player, context.NearbyUids);

            foreach (var packet in EntityBroadcast.CreateList(
                Clock.FixedFrame,
                context.Headers,
                context.PlayerFixedIndexes))
            {
                result.Add((nickname, packet));
            }
        }

        foreach (var (nickname, packet) in result.OrderBy(_ => RandUtils.NextInt()))
        {
            Server.SendFast(nickname, packet);
        }
    }

    private PlayerContext BuildPlayerContext(ulong playerUID, Vec2 playerPos)
    {
        var headers = new Dictionary<ulong, EntityHeader>();
        var fixedIndexes = new Dictionary<ulong, uint>();
        var nearby = new HashSet<ulong>();

        foreach (ulong uid in EntityControllers.Uids)
        {
            if (uid == playerUID) continue;

            var controller = EntityControllers.GetController(uid)!;

            if (Vec2.Distance(playerPos, controller.Position) >= Common.ViewDistance)
                continue;

            if (controller.IsActive)
            {
                headers[uid] = controller.ActiveData.Header;
                nearby.Add(uid);

                if (controller is PlayerController pc)
                {
                    var update = ConnectedPlayers[pc.Nickname].LastUpdate!;
                    fixedIndexes[uid] = update.FixedFrame;
                }
            }
            else if (controller is not PlayerController)
            {
                nearby.Add(uid);
            }
        }

        return new PlayerContext(headers, fixedIndexes, nearby);
    }

    private void SendNearbyDiff(string nickname, JoinedPlayer player, HashSet<ulong> newUids)
    {
        var old = player.NearbyEntityUids;

        ulong[] toAdd = newUids.Except(old).ToArray();
        ulong[] toRemove = old.Except(newUids).ToArray();

        var packets = NearbyEntities.CreateList(Clock.FixedFrame, toAdd, toRemove);

        if (Clock.FixedFrame % 6 == 0 && packets.Count == 0)
        {
            packets.Add(NearbyEntities.CreateBootstrap(Clock.FixedFrame));
        }

        foreach (var p in packets)
        {
            Server.Send(nickname, p);
        }

        player.NearbyEntityUids = newUids;
    }

    private void SendFrameInfo()
    {
        foreach (string nickname in ConnectedPlayers.AllPlayers)
        {
            var player = ConnectedPlayers[nickname];
            Vec2 position = player.RenderPosition;

            FrameInfo packet = new(
                serverTick: Clock.ServerTick,
                skyColor: Generator.SkyColorAt(position),
                biomeID: Generator.BiomeAt(position),
                weather: WeatherID.Clear,
                tps: Clock.TPS
            );

            Server.SendFast(nickname, packet);
        }
    }
}
