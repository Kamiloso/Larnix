using System;
using System.Collections;
using Larnix.Blocks;
using QuickNet;
using QuickNet.Channel;
using UnityEngine;

namespace Larnix.Packets
{
    public class BlockChange : Payload
    {
        private const int SIZE = (4 + 4) + 5 + 8 + 1;

        public Vector2Int BlockPosition => new Vector2Int(
            EndianUnsafe.FromBytes<int>(Bytes, 0),  // 4B
            EndianUnsafe.FromBytes<int>(Bytes, 4)); // 4B
        public SingleBlockData Item => BlockData.Deserialize(Bytes, 8).Front; // 2.5B
        public SingleBlockData Tool => BlockData.Deserialize(Bytes, 8).Back; // 2.5B
        public long Operation => EndianUnsafe.FromBytes<long>(Bytes, 13); // 8B
        public bool Front => (Bytes[21] & 0b1) != 0; // 1B


        public BlockChange() { }
        public BlockChange(Vector2Int blockPosition, SingleBlockData item, SingleBlockData tool, long operation, bool front, byte code = 0)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (tool == null) throw new ArgumentNullException(nameof(tool));

            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(blockPosition.x),
                EndianUnsafe.GetBytes(blockPosition.y),
                new BlockData(item, tool).Serialize(),
                EndianUnsafe.GetBytes(operation),
                new byte[] { (byte)(front ? 0b1 : 0b0) }
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes?.Length == SIZE &&
                BlockPosition.x >= ChunkMethods.MIN_BLOCK && BlockPosition.x <= ChunkMethods.MAX_BLOCK &&
                BlockPosition.y >= ChunkMethods.MIN_BLOCK && BlockPosition.y <= ChunkMethods.MAX_BLOCK;
        }
    }
}
