using System;
using System.Collections;
using Larnix.Blocks;
using QuickNet;
using QuickNet.Channel;
using UnityEngine;

namespace Larnix.Packets
{
    public class ChunkInfo : Payload
    {
        private const int SIZE = (4 + 4) + (16 * 16 * 5);
        private const int ALT_SIZE = (4 + 4);

        public Vector2Int Chunkpos => new Vector2Int(
            EndianUnsafe.FromBytes<int>(Bytes, 0),  // 4B
            EndianUnsafe.FromBytes<int>(Bytes, 4)); // 4B
        public BlockData[,] Blocks => (Bytes.Length != ALT_SIZE ? ChunkMethods.DeserializeChunk(Bytes, 8) : null); // 1280B or 0B

        public ChunkInfo() { }
        public ChunkInfo(Vector2Int chunkpos, BlockData[,] blocks, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(chunkpos.x),
                EndianUnsafe.GetBytes(chunkpos.y),
                blocks != null ? ChunkMethods.SerializeChunk(blocks) : new byte[0]
                ), code);
        }

        protected override bool IsValid()
        {
            return (Bytes?.Length == SIZE || Bytes?.Length == ALT_SIZE) &&
                Chunkpos.x >= ChunkMethods.MIN_CHUNK && Chunkpos.x <= ChunkMethods.MAX_CHUNK &&
                Chunkpos.y >= ChunkMethods.MIN_CHUNK && Chunkpos.y <= ChunkMethods.MAX_CHUNK;
        }
    }
}
