using System;
using System.Collections;
using Larnix.Blocks;
using QuickNet;
using QuickNet.Channel;
using UnityEngine;

namespace Larnix.Packets
{
    public class RetBlockChange : Payload
    {
        private const int SIZE = (4 + 4) + 8 + 5 + 1;

        public Vector2Int BlockPosition => new Vector2Int(
            EndianUnsafe.FromBytes<int>(Bytes, 0),  // 4B
            EndianUnsafe.FromBytes<int>(Bytes, 4)); // 4B
        public long Operation => EndianUnsafe.FromBytes<long>(Bytes, 8); // 8B
        public BlockData CurrentBlock => BlockData.Deserialize(Bytes, 16); // 5B
        public bool Front => (Bytes[21] & 0b01) != 0; // flag
        public bool Success => (Bytes[21] & 0b10) != 0; // flag

        public RetBlockChange() { }
        public RetBlockChange(Vector2Int blockPosition, long operation, BlockData currentBlock, bool front, bool success, byte code = 0)
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
                BlockPosition.x >= ChunkMethods.MIN_BLOCK && BlockPosition.x <= ChunkMethods.MAX_BLOCK &&
                BlockPosition.y >= ChunkMethods.MIN_BLOCK && BlockPosition.y <= ChunkMethods.MAX_BLOCK;
        }
    }
}
