using Larnix.Core.Vectors;
using Larnix.Model.Utils;
using Larnix.Socket.Packets;
using Larnix.Model.Blocks.Structs;
using Larnix.Core.Utils;
using Larnix.Core;

namespace Larnix.Server.Packets;

public sealed class RetBlockChange : Payload
{
    private static int SIZE => Binary<Vec2Int>.Size + sizeof(long) + Binary<BlockHeader2>.Size + sizeof(byte);

    public Vec2Int BlockPosition => Binary<Vec2Int>.Deserialize(Bytes, 0); // Binary<Vec2>.Size
    public long Operation => Binary<long>.Deserialize(Bytes, 8); // sizeof(long)
    public BlockHeader2 CurrentBlock => Binary<BlockHeader2>.Deserialize(Bytes, 16); // BlockHeader2.SIZE
    public bool Front => (Bytes[21] & 0b01) != 0; // flag
    public bool Success => (Bytes[21] & 0b10) != 0; // flag

    public RetBlockChange(Vec2Int blockPosition, long operation, BlockHeader2 currentBlock, bool front, bool success, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<Vec2Int>.Serialize(blockPosition),
            Binary<long>.Serialize(operation),
            Binary<BlockHeader2>.Serialize(currentBlock),
            new byte[] { (byte)((front ? 0b01 : 0b00) | (success ? 0b10 : 0b00)) }
            ), code);
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE &&
            BlockPosition.x >= BlockUtils.MIN_BLOCK && BlockPosition.x <= BlockUtils.MAX_BLOCK &&
            BlockPosition.y >= BlockUtils.MIN_BLOCK && BlockPosition.y <= BlockUtils.MAX_BLOCK;
    }
}
