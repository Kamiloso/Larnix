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
        private const int SIZE = 8 + 5 + 8 + 1;

        public Vec2Int BlockPosition => Structures.FromBytes<Vec2Int>(Bytes, 0); // 8B
        public BlockData1 Item => Structures.FromBytes<BlockData2>(Bytes, 8).Front; // 2.5B
        public BlockData1 Tool => Structures.FromBytes<BlockData2>(Bytes, 8).Back; // 2.5B
        public long Operation => Primitives.FromBytes<long>(Bytes, 13); // 8B
        public bool Front => (Bytes[21] & 0b1) != 0; // 1B


        public BlockChange() { }
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
