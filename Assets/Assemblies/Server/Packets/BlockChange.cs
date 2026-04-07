#nullable enable
using Larnix.Model.Utils;
using Larnix.Core.Vectors;
using Larnix.Socket.Packets;
using Larnix.Model.Blocks.Structs;
using Larnix.Core.Utils;
using Larnix.Core;

namespace Larnix.Server.Packets;

public sealed class BlockChange : Payload
{
    private static int SIZE => Binary<Vec2Int>.Size + Binary<BlockHeader2>.Size + sizeof(long) + sizeof(byte);

    public Vec2Int BlockPosition => Binary<Vec2Int>.Deserialize(Bytes, 0);
    public BlockHeader1 Item => Binary<BlockHeader2>.Deserialize(Bytes, 8).Front;
    public BlockHeader1 Tool => Binary<BlockHeader2>.Deserialize(Bytes, 8).Back;
    public long Operation => Binary<long>.Deserialize(Bytes, 13);
    public bool Front => (Bytes[21] & 0b1) != 0;

    public BlockChange(Vec2Int blockPosition, BlockHeader1 item, BlockHeader1 tool, long operation, bool front, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<Vec2Int>.Serialize(blockPosition),
            Binary<BlockHeader2>.Serialize(new BlockHeader2(item, tool)),
            Binary<long>.Serialize(operation),
            new byte[] { (byte)(front ? 0b1 : 0b0) }
            ), code);
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE &&
            BlockPosition.x >= BlockUtils.MIN_BLOCK && BlockPosition.x <= BlockUtils.MAX_BLOCK &&
            BlockPosition.y >= BlockUtils.MIN_BLOCK && BlockPosition.y <= BlockUtils.MAX_BLOCK;
    }
}
