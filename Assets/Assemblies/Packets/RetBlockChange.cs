using System;
using System.Collections;
using Larnix.Blocks;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;
using Larnix.Core.Binary;
using Larnix.Socket.Packets;

namespace Larnix.Packets
{
    public sealed class RetBlockChange : Payload
    {
        private const int SIZE = Vec2Int.SIZE + sizeof(long) + BlockData2.SIZE + sizeof(byte);

        public Vec2Int BlockPosition => Structures.FromBytes<Vec2Int>(Bytes, 0); // Vec2Int.SIZE
        public long Operation => Primitives.FromBytes<long>(Bytes, Vec2Int.SIZE); // sizeof(long)
        public BlockData2 CurrentBlock => Structures.FromBytes<BlockData2>(Bytes, Vec2Int.SIZE + sizeof(long)); // BlockData2.SIZE
        public bool Front => (Bytes[Vec2Int.SIZE + sizeof(long) + BlockData2.SIZE] & 0b01) != 0; // flag
        public bool Success => (Bytes[Vec2Int.SIZE + sizeof(long) + BlockData2.SIZE] & 0b10) != 0; // flag

        public RetBlockChange() { }
        public RetBlockChange(Vec2Int blockPosition, long operation, BlockData2 currentBlock, bool front, bool success, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                Structures.GetBytes(blockPosition),
                Primitives.GetBytes(operation),
                Structures.GetBytes(currentBlock),
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
}
