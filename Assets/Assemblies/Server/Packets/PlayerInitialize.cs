#nullable enable
using Larnix.Core.Vectors;
using Larnix.Socket.Packets;
using Larnix.Core.Utils;
using Larnix.Core;

namespace Larnix.Server.Packets;

public sealed class PlayerInitialize : Payload
{
    private static int SIZE => Binary<Vec2>.Size + sizeof(ulong) + sizeof(uint);

    public Vec2 Position => Binary<Vec2>.Deserialize(Bytes, 0);
    public ulong MyUid => Binary<ulong>.Deserialize(Bytes, 16);
    public uint LastFixedFrame => Binary<uint>.Deserialize(Bytes, 24);

    public PlayerInitialize(Vec2 position, ulong myUid, uint lastFixedFrame, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<Vec2>.Serialize(position),
            Binary<ulong>.Serialize(myUid),
            Binary<uint>.Serialize(lastFixedFrame)
            ), code);
    }

    protected override bool IsValid()
    {
		return Bytes.Length == SIZE;
    }
}
