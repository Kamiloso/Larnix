#nullable enable
using Larnix.Core.Vectors;
using Larnix.Socket.Packets;
using Larnix.Core.Utils;
using Larnix.Core;

namespace Larnix.Server.Packets;

public sealed class PlayerUpdate : Payload
{
    private static int SIZE => Binary<Vec2>.Size + sizeof(float) + sizeof(uint);

    public Vec2 Position => Binary<Vec2>.Deserialize(Bytes, 0); // Binary<Vec2>.Size
    public float Rotation => Binary<float>.Deserialize(Bytes, 16); // sizeof(float)
    public uint FixedFrame => Binary<uint>.Deserialize(Bytes, 20); // sizeof(uint)

    public PlayerUpdate(Vec2 position, float rotation, uint fixedFrame, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<Vec2>.Serialize(position),
            Binary<float>.Serialize(rotation),
            Binary<uint>.Serialize(fixedFrame)
            ), code);
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE;
    }
}
