#nullable enable
using Larnix.Core.Vectors;
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(0x06)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Teleport : ISanitizable<Teleport>
{
    public Vec2 Position { get; }

    public Teleport(Vec2 position)
    {
        Position = position.Sanitize();
    }

    public Teleport Sanitize()
    {
        return new Teleport(Position);
    }
}
