using Larnix.Core.Binary;
using Larnix.Core.Vectors;
using BreakMode = Larnix.Model.Blocks.IWorldAPI.BreakMode;
using Larnix.Model.Blocks.Structs;
using Larnix.Core.Utils;

namespace Larnix.Server.Packets.Structs;

public readonly struct BlockUpdateRecord : IBinary<BlockUpdateRecord>
{
    public const int SIZE = Vec2Int.SIZE + BlockHeader2.SIZE + sizeof(BreakMode);

    public Vec2Int Position { get; }
    public BlockHeader2 Block { get; }
    public BreakMode BreakMode { get; }

    public BlockUpdateRecord(Vec2Int position, BlockHeader2 block, BreakMode breakMode)
    {
        Position = position;
        Block = block;
        BreakMode = breakMode;
    }

    public byte[] Serialize()
    {
        return ArrayUtils.MegaConcat(
            Structures.GetBytes(Position),
            Structures.GetBytes(Block),
            Primitives.GetBytes(BreakMode)
            );
    }

    public bool Deserialize(byte[] bytes, int offset, out BlockUpdateRecord result)
    {
        if (offset < 0 || offset + SIZE > bytes.Length)
        {
            result = default;
            return false;
        }

        Vec2Int position = Structures.FromBytes<Vec2Int>(bytes, offset);
        offset += Vec2Int.SIZE;

        BlockHeader2 block = Structures.FromBytes<BlockHeader2>(bytes, offset);
        offset += BlockHeader2.SIZE;

        BreakMode breakMode = (BreakMode)bytes[offset];
        offset += sizeof(BreakMode);

        result = new BlockUpdateRecord(position, block, breakMode);
        return true;
    }
}
