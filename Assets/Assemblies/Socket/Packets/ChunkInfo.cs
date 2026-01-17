using System;
using System.Collections;
using Larnix.Blocks;
using UnityEngine;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;
using Larnix.Socket.Structs;

namespace Larnix.Socket.Packets
{
    public class ChunkInfo : Payload
    {
        private const int MIN_SIZE = 4 + 4;
        private const int MAX_SIZE = 4 + 4 + 16 * 16 * 5;

        public Vector2Int Chunkpos => new Vector2Int(
            EndianUnsafe.FromBytes<int>(Bytes, 0),  // 4B
            EndianUnsafe.FromBytes<int>(Bytes, 4)); // 4B
        public BlockData2[,] Blocks => Bytes.Length != MIN_SIZE ? ChunkMethods.DeserializeChunk(Bytes, 8) : null; // 0B - 1280B

        public ChunkInfo() { }
        public ChunkInfo(Vector2Int chunkpos, BlockData2[,] blocks, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(chunkpos.x),
                EndianUnsafe.GetBytes(chunkpos.y),
                blocks != null ? ChunkMethods.SerializeChunk(blocks) : new byte[0]
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes?.Length >= MIN_SIZE && Bytes?.Length <= MAX_SIZE &&
                Chunkpos.x >= BlockUtils.MIN_CHUNK && Chunkpos.x <= BlockUtils.MAX_CHUNK &&
                Chunkpos.y >= BlockUtils.MIN_CHUNK && Chunkpos.y <= BlockUtils.MAX_CHUNK;
        }
    }
}
