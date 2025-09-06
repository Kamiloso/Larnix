using System;
using System.Collections;
using UnityEngine;

namespace Larnix.Blocks
{
    public static class ChunkMethods
    {
        public const int MIN_CHUNK = -(1 << 27) + 1;
        public const int MAX_CHUNK = -(MIN_CHUNK + 1);

        public const int MIN_BLOCK = MIN_CHUNK * 16;
        public const int MAX_BLOCK = -(MIN_BLOCK + 1);

        public static Vector2Int CoordsToChunk(Vec2 position)
        {
            return CoordsToBlock(position, 16.0);
        }

        public static Vector2Int CoordsToChunk(Vector2Int intPosition)
        {
            return new Vector2Int(
                intPosition.x >> 4,
                intPosition.y >> 4
                );
        }

        public static Vector2Int CoordsToBlock(Vec2 position, double blockSize = 1.0)
        {
            try
            {
                return new Vector2Int(
                    (int)Math.Floor((position.x + 0.5) / blockSize),
                    (int)Math.Floor((position.y + 0.5) / blockSize)
                );
            }
            catch (OverflowException)
            {
                return default;
            }
        }

        public static Vector2Int GlobalBlockCoords(Vector2Int chunkpos, Vector2Int pos)
        {
            return new Vector2Int(chunkpos.x << 4, chunkpos.y << 4) + pos;
        }

        public static Vector2Int LocalBlockCoords(Vector2Int POS)
        {
            int x = POS.x & 0b1111;
            int y = POS.y & 0b1111;
            return new Vector2Int(x, y);
        }

        public static Vec2 ChunkCenter(Vector2Int chunkpos)
        {
            return new Vec2(chunkpos.x << 4, chunkpos.y << 4) + new Vec2(7.5, 7.5);
        }

        public static bool InChunk(Vector2Int chunkpos, Vector2Int POS)
        {
            return (POS.x >> 4) == chunkpos.x && (POS.y >> 4) == chunkpos.y;
        }

        public static BlockData2[,] DeserializeChunk(byte[] bytes, int offset = 0)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length - offset < 1280) throw new ArgumentException("Cannot convert bytes to chunk!");

            BlockData2[,] blocks = new BlockData2[16, 16];
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    blocks[x, y] = BlockData2.Deserialize(bytes, offset + (16 * x + y) * 5);
                }

            return blocks;
        }

        public static byte[] SerializeChunk(BlockData2[,] blocks)
        {
            if (blocks == null) throw new ArgumentNullException(nameof(blocks));
            if (blocks.GetLength(0) != 16 || blocks.GetLength(1) != 16) throw new ArgumentException("Blocks array must be 16 x 16.");

            byte[] bytes = new byte[1280];
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    byte[] arr = blocks[x, y]?.Serialize() ?? throw new NullReferenceException("Array elements cannot be null!");
                    Buffer.BlockCopy(arr, 0, bytes, (16 * x + y) * 5, 5);
                }

            return bytes;
        }
    }
}
