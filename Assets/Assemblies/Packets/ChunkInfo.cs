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
    public sealed class ChunkInfo : Payload
    {
        private const int MIN_SIZE = Vec2Int.SIZE;
        private const int MAX_SIZE = Vec2Int.SIZE + 16 * 16 * BlockData2.SIZE;

        public Vec2Int Chunkpos => Structures.FromBytes<Vec2Int>(Bytes, 0); // Vec2Int.SIZE
        public BlockData2[,] Blocks => Bytes.Length != MIN_SIZE ? ChunkMethods.DeserializeChunk(Bytes, Vec2Int.SIZE) : null; // 0B - 1280B

        public ChunkInfo() { }
        public ChunkInfo(Vec2Int chunkpos, BlockData2[,] blocks, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                Structures.GetBytes(chunkpos),
                blocks?.SerializeChunk() ?? new byte[0]
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes.Length >= MIN_SIZE && Bytes.Length <= MAX_SIZE &&
                Chunkpos.x >= BlockUtils.MIN_CHUNK && Chunkpos.x <= BlockUtils.MAX_CHUNK &&
                Chunkpos.y >= BlockUtils.MIN_CHUNK && Chunkpos.y <= BlockUtils.MAX_CHUNK;
        }
    }
}
