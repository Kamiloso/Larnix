using System;
using Larnix.Core.Binary;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Core.Utils;
using Larnix.Blocks;

namespace Larnix.Packets.Structs
{
    public class BlockUpdateRecord : IBinary<BlockUpdateRecord>
    {
        public const int SIZE = Vec2Int.SIZE + BlockData2.SIZE + sizeof(IWorldAPI.BreakMode);

        public Vec2Int Position { get; private set; }
        public BlockData2 Block { get; private set; }
        public IWorldAPI.BreakMode BreakMode { get; private set; }

        public BlockUpdateRecord() => Block = new();
        public BlockUpdateRecord(Vec2Int position, BlockData2 block, IWorldAPI.BreakMode breakMode)
        {
            Position = position;
            Block = block ?? new();
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

        public bool Deserialize(byte[] bytes, int offset = 0)
        {
            if (offset + SIZE > bytes.Length)
                return false;

            Position = Structures.FromBytes<Vec2Int>(bytes, offset);
            offset += Vec2Int.SIZE;

            Block = Structures.FromBytes<BlockData2>(bytes, offset);
            offset += BlockData2.SIZE;

            BreakMode = (IWorldAPI.BreakMode)bytes[offset];
            offset += sizeof(IWorldAPI.BreakMode);

            return true;
        }
    }
}
