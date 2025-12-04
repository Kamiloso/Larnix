using System;
using UnityEngine;
using Larnix.Core.Serialization;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;
using Larnix.Socket.Structs;

namespace Larnix.Socket.Packets
{
    public class BlockChange : Payload
    {
        private const int SIZE = (4 + 4) + 5 + 8 + 1;

        public Vector2Int BlockPosition => new Vector2Int(
            EndianUnsafe.FromBytes<int>(Bytes, 0),  // 4B
            EndianUnsafe.FromBytes<int>(Bytes, 4)); // 4B
        public BlockData1 Item => BlockData2.Deserialize(Bytes, 8).Front; // 2.5B
        public BlockData1 Tool => BlockData2.Deserialize(Bytes, 8).Back; // 2.5B
        public long Operation => EndianUnsafe.FromBytes<long>(Bytes, 13); // 8B
        public bool Front => (Bytes[21] & 0b1) != 0; // 1B


        public BlockChange() { }
        public BlockChange(Vector2Int blockPosition, BlockData1 item, BlockData1 tool, long operation, bool front, byte code = 0)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (tool == null) throw new ArgumentNullException(nameof(tool));

            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(blockPosition.x),
                EndianUnsafe.GetBytes(blockPosition.y),
                new BlockData2(item, tool).Serialize(),
                EndianUnsafe.GetBytes(operation),
                new byte[] { (byte)(front ? 0b1 : 0b0) }
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
