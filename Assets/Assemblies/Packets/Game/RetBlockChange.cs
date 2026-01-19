using System;
using System.Collections;
using Larnix.Blocks;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;
using Larnix.Core.Binary;

namespace Larnix.Packets.Game
{
    public class RetBlockChange : Payload
    {
        private const int SIZE = (4 + 4) + 8 + 5 + 1;

        public Vec2Int BlockPosition => new Vec2Int(
            EndianUnsafe.FromBytes<int>(Bytes, 0),  // 4B
            EndianUnsafe.FromBytes<int>(Bytes, 4)); // 4B
        public long Operation => EndianUnsafe.FromBytes<long>(Bytes, 8); // 8B
        public BlockData2 CurrentBlock => BlockData2.Deserialize(Bytes, 16); // 5B
        public bool Front => (Bytes[21] & 0b01) != 0; // flag
        public bool Success => (Bytes[21] & 0b10) != 0; // flag

        public RetBlockChange() { }
        public RetBlockChange(Vec2Int blockPosition, long operation, BlockData2 currentBlock, bool front, bool success, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(blockPosition.x),
                EndianUnsafe.GetBytes(blockPosition.y),
                EndianUnsafe.GetBytes(operation),
                currentBlock?.Serialize() ?? throw new ArgumentNullException(nameof(currentBlock)),
                new byte[] { (byte)((front ? 0b01 : 0b00) | (success ? 0b10 : 0b00)) }
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes?.Length == SIZE &&
                BlockPosition.x >= BlockUtils.MIN_BLOCK && BlockPosition.x <= BlockUtils.MAX_BLOCK &&
                BlockPosition.y >= BlockUtils.MIN_BLOCK && BlockPosition.y <= BlockUtils.MAX_BLOCK;
        }
    }
}
