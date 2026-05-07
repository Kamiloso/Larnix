#nullable enable
using Larnix.Core.Vectors;
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Model.Packets;

[CmdId(9)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct PlayerUpdate : ISanitizable<PlayerUpdate>
{
    public Vec2 Position { get; }
    public float Rotation { get; }
    public uint FixedFrame { get; }

    public PlayerUpdate(Vec2 position, float rotation, uint fixedFrame)
    {
        Position = Sanitizer.Filter(position);
        Rotation = Sanitizer.ToFinite(rotation);
        FixedFrame = fixedFrame;
    }

    public PlayerUpdate Sanitize()
    {
        return new PlayerUpdate(Position, Rotation, FixedFrame);
    }
}
