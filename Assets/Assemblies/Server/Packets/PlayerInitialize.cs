#nullable enable
using Larnix.Core.Serialization;
using Larnix.Core.Vectors;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(0x0B)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct PlayerInitialize : ISanitizable<PlayerInitialize>
{
    public Vec2 Position { get; }
    public ulong Uid { get; }
    public uint LastFixedFrame { get; }

    public PlayerInitialize(Vec2 position, ulong uid, uint lastFixedFrame)
    {
        Position = position.Sanitize();
        Uid = uid;
        LastFixedFrame = lastFixedFrame;
    }

    public PlayerInitialize Sanitize()
    {
        return new PlayerInitialize(Position, Uid, LastFixedFrame);
    }
}
