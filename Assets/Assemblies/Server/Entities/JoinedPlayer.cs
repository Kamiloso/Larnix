#nullable enable
using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Server.Entities.Controllers;
using Larnix.Server.Packets;
using System.Collections.Generic;

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

    public HashSet<ulong> NearbyEntityUids { get; set; } = new();
    public HashSet<Vec2Int> LoadedChunks { get; set; } = new();

    private PlayerController? PlayerController => EntityControllers.GetController(Uid) as PlayerController;

    public PlayerState State
    {
        get
        {
            if (PlayerController is null)
                return PlayerState.Dead;

            return PlayerController.IsActive
                ? PlayerState.Alive
                : PlayerState.Inactive;
        }
    }

    public Vec2 RenderPosition => PlayerController?.Position ?? LastUpdate!.Position;
    public PlayerUpdate? LastUpdate { get; private set; }

    private IEntityControllers EntityControllers => GlobRef.Get<IEntityControllers>();

    public JoinedPlayer(ulong uid, string nickname)
    {
        Uid = uid;
        Nickname = nickname;
    }

    public void Update(PlayerUpdate msg)
    {
        if (PlayerController is not null)
        {
            PlayerController.UpdateTransform(msg);
            LastUpdate = msg;
        }
    }
}
