#nullable enable
using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Server.Entities.Controllers;
using Larnix.Model.Packets;
using System.Collections.Generic;
using System.Net;

namespace Larnix.Server.Entities;

internal enum PlayerState : byte
{
    None, // not connected, no controller, no anything (even no ConnPlayer object)
    Inactive, // controller present, but not active yet (waiting for first update packet)
    Alive, // alive and somewhere in the world
    Dead // controller doesn't exist, but player is connected and has a rendering position
}

internal class JoinedPlayer
{
    public ulong Uid { get; }
    public string Nickname { get; }
    public IPEndPoint EndPoint { get; }

    public HashSet<ulong> NearbyEntityUids { get; set; } = new();
    public HashSet<Vec2Int> LoadedChunks { get; set; } = new();

    public bool HasPlayerUpdate => _lastUpdate is not null;
    public Vec2 RenderPosition => GetPlayerController()?.Position ?? _lastUpdate?.Position ?? Vec2.Zero;
    public uint FixedFrame => _lastUpdate?.FixedFrame ?? 0;

    private PlayerUpdate? _lastUpdate;

    private IEntityControllers EntityControllers => GlobRef.Get<IEntityControllers>();

    public JoinedPlayer(ulong uid, string nickname, IPEndPoint endpoint)
    {
        Uid = uid;
        Nickname = nickname;
        EndPoint = endpoint;
    }

    public void Update(in PlayerUpdate msg)
    {
        var controller = GetPlayerController();

        if (controller is not null)
        {
            controller.UpdateTransform(msg);
            _lastUpdate = msg;
        }
    }

    public PlayerState GetState()
    {
        var controller = GetPlayerController();

        if (controller is null)
            return PlayerState.Dead;

        return controller.IsActive
            ? PlayerState.Alive
            : PlayerState.Inactive;
    }

    private PlayerController? GetPlayerController()
    {
        return EntityControllers.GetController(Uid) as PlayerController;
    }
}
