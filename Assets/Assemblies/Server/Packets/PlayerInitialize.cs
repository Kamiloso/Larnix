#nullable enable
using Larnix.Core.Serialization;
using Larnix.Core.Vectors;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(8)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct PlayerInitialize : ISanitizable<PlayerInitialize>
{
    public Vec2 Position { get; }
    public ulong Uid { get; }
    public uint LastFixedFrame { get; }

    public PlayerInitialize(Vec2 position, ulong uid, uint lastFixedFrame)
    {
        Position = Sanitizer.Filter(position);
        Uid = uid;
        LastFixedFrame = lastFixedFrame;
    }

    public PlayerInitialize Sanitize()
    {
        return new PlayerInitialize(Position, Uid, LastFixedFrame);
    }
}
