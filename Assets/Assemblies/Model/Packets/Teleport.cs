#nullable enable
using Larnix.Core.Vectors;
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Model.Packets;

[CmdId(12)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Teleport : ISanitizable<Teleport>
{
    public Vec2 Position { get; }

    public Teleport(Vec2 position)
    {
        Position = Sanitizer.Filter(position);
    }

    public Teleport Sanitize()
    {
        return new Teleport(Position);
    }
}
