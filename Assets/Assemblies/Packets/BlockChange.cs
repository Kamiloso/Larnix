using System;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;
using Larnix.Core.Binary;
using Larnix.Core.Vectors;
using Larnix.Socket.Packets;

namespace Larnix.Packets
{
    public sealed class BlockChange : Payload
    {
        private const int SIZE = Vec2Int.SIZE + BlockData2.SIZE + sizeof(long) + sizeof(byte);

        public Vec2Int BlockPosition => Structures.FromBytes<Vec2Int>(Bytes, 0); // Vec2Int.SIZE
        public BlockData1 Item => Structures.FromBytes<BlockData2>(Bytes, Vec2Int.SIZE).Front; // BlockData2.SIZE
        public BlockData1 Tool => Structures.FromBytes<BlockData2>(Bytes, Vec2Int.SIZE).Back; // BlockData2.SIZE
        public long Operation => Primitives.FromBytes<long>(Bytes, Vec2Int.SIZE + BlockData2.SIZE); // sizeof(long)
        public bool Front => (Bytes[Vec2Int.SIZE + BlockData2.SIZE + sizeof(long)] & 0b1) != 0; // flag


        public BlockChange(Vec2Int blockPosition, BlockData1 item, BlockData1 tool, long operation, bool front, byte code = 0)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (tool == null) throw new ArgumentNullException(nameof(tool));

            InitializePayload(ArrayUtils.MegaConcat(
                Structures.GetBytes(blockPosition),
                Structures.GetBytes(new BlockData2(item, tool)),
                Primitives.GetBytes(operation),
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
}
