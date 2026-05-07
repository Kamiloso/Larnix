#nullable enable
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Model;
using Larnix.Model.Entities.Structs;
using Larnix.Model.Enums;
using Larnix.Model.Worldgen;
using Larnix.Server.Entities;
using Larnix.Server.Entities.Controllers;
using Larnix.Model.Packets;
using Larnix.Server.Packets.Structs;
using System.Collections.Generic;
using System.Linq;
using Larnix.Server.Network;
using Larnix.Server;

internal interface IEntitySender : IScript { }

internal class EntitySender : IEntitySender
{
    private Config Config => GlobRef.Get<Config>();
    private IServer Server => GlobRef.Get<IServer>();
    private IClock Clock => GlobRef.Get<IClock>();
    private IEntityControllers EntityControllers => GlobRef.Get<IEntityControllers>();
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();
    private IGenerator Generator => GlobRef.Get<IGenerator>();

    private record PlayerContext(
        List<BroadcastRecord> Broadcasts,
        HashSet<ulong> NearbyUids
    );

    void IScript.PostLateUpdate()
    {
        if (Clock.FixedFrame % Config.PeriodicTasks_EntityBroadcastPeriodFrames == 0)
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
            JoinedPlayer player = ConnectedPlayers[nickname];

            ulong uid = ConnectedPlayers.UidByNickname(nickname);
            Vec2 position = player.RenderPosition;

            var context = BuildPlayerContext(uid, position);

            SendNearbyDiff(nickname, player, context.NearbyUids);

            var payloads = EntityBroadcast.CreateList(
                Clock.FixedFrame,
                context.Broadcasts.ToArray()
                );

            foreach (var packet in payloads)
            {
                result.Add((nickname, packet));
            }
        }

        foreach (var (nickname, packet) in result.OrderBy(_ => RandUtils.NextInt()))
        {
            Server.SendUnreliable(nickname, packet);
        }
    }

    private PlayerContext BuildPlayerContext(ulong playerUID, Vec2 playerPos)
    {
        var broadcasts = new List<BroadcastRecord>();
        var nearby = new HashSet<ulong>();

        foreach (ulong uid in EntityControllers.Uids)
        {
            if (uid == playerUID) continue;

            var controller = EntityControllers.GetController(uid)!;

            if (Vec2.Distance(playerPos, controller.Position) >= Common.ViewDistance)
                continue;

            if (controller.IsActive)
            {
                nearby.Add(uid);

                EntityHeader header = controller.ActiveData.Header;

                broadcasts.Add(controller is PlayerController pc
                    ? BroadcastRecord.CreatePlayer(uid, header, ConnectedPlayers[pc.Nickname].FixedFrame)
                    : BroadcastRecord.CreateEntity(uid, header)
                );
            }
        }

        return new PlayerContext(broadcasts, nearby);
    }

    private void SendNearbyDiff(string nickname, JoinedPlayer player, HashSet<ulong> newUids)
    {
        var old = player.NearbyEntityUids;

        ulong[] toAdd = newUids.Except(old).ToArray();
        ulong[] toRemove = old.Except(newUids).ToArray();

        var payloads = NearbyEntities.CreateList(Clock.FixedFrame, toAdd, toRemove).ToList();

        if (Clock.FixedFrame % 6 == 0 && payloads.Count == 0)
        {
            payloads.Add(NearbyEntities.CreateBootstrap(Clock.FixedFrame));
        }

        payloads.ForEach(payload => Server.Send(nickname, payload));

        player.NearbyEntityUids = newUids;
    }

    private void SendFrameInfo()
    {
        foreach (string nickname in ConnectedPlayers.AllPlayers)
        {
            var player = ConnectedPlayers[nickname];
            Vec2 position = player.RenderPosition;

            var payload = new FrameInfo(
                serverTick: Clock.ServerTick,
                skyColor: Generator.SkyColorAt(position),
                biomeId: Generator.BiomeAt(position),
                weatherId: WeatherID.Clear,
                tps: Clock.TPS
            );

            Server.SendUnreliable(nickname, payload);
        }
    }
}
