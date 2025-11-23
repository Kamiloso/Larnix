using Socket;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Core.Vectors;

namespace Larnix.Blocks
{
    public static class ChunkMethods
    {
        public const int MIN_CHUNK = -(1 << 27) + 1;
        public const int MAX_CHUNK = -(MIN_CHUNK + 1);

        public const int MIN_BLOCK = MIN_CHUNK * 16;
        public const int MAX_BLOCK = -(MIN_BLOCK + 1);

        public const int LOADING_DISTANCE = 2; // chunks

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

        public static bool InChunk(Vector2Int chunkpos, Vector2Int POS)
        {
            return (POS.x >> 4) == chunkpos.x && (POS.y >> 4) == chunkpos.y;
        }

        public static HashSet<Vector2Int> GetCenterChunks(List<Vec2> positions)
        {
            HashSet<Vector2Int> returns = new();
            foreach (Vec2 pos in positions)
            {
                returns.Add(CoordsToChunk(pos));
            }
            return returns;
        }

        public static HashSet<Vector2Int> GetNearbyChunks(Vector2Int center, int simDistance)
        {
            int min_x = System.Math.Clamp(center.x - simDistance, MIN_CHUNK, int.MaxValue);
            int min_y = System.Math.Clamp(center.y - simDistance, MIN_CHUNK, int.MaxValue);
            int max_x = System.Math.Clamp(center.x + simDistance, int.MinValue, MAX_CHUNK);
            int max_y = System.Math.Clamp(center.y + simDistance, int.MinValue, MAX_CHUNK);

            HashSet<Vector2Int> returns = new();
            for (int x = min_x; x <= max_x; x++)
                for (int y = min_y; y <= max_y; y++)
                {
                    returns.Add(new Vector2Int(x, y));
                }

            return returns;
        }

        public static byte[] SerializeChunk(BlockData2[,] blocks)
        {
            if (blocks == null) throw new ArgumentNullException(nameof(blocks));
            if (blocks.GetLength(0) != 16 || blocks.GetLength(1) != 16) throw new ArgumentException("Blocks array must be 16 x 16.");

            Func<byte, BlockData2> BlockIndexer = b => blocks[b / 16, b % 16];

            for (int i = 0; i < 256; i++)
            {
                if (BlockIndexer((byte)i) == null)
                    throw new NullReferenceException("Array elements cannot be null!");
            }

            Dictionary<long, byte> blockMap = new();
            byte[] bytes = new byte[1280];
            int eyes = 1;

            // make dictionary
            for (int i = 0; i < 256; i++)
            {
                BlockData2 bd2 = BlockIndexer((byte)i);
                if (blockMap.TryAdd(bd2.UniqueLong(), bytes[0]))
                {
                    if (eyes + 5 >= 1280)
                        goto fallback_to_raw;

                    byte[] blockBytes = bd2.Serialize();
                    Buffer.BlockCopy(blockBytes, 0, bytes, eyes, 5);
                    eyes += 5;
                    bytes[0]++; // can overflow to 256 = 0
                }
            }

            // fill with data
            long? previous = null;
            for (int i = 0; i < 256; i++)
            {
                BlockData2 bd2 = BlockIndexer((byte)i);
                long current = bd2.UniqueLong();

                if (previous != current) // next pair
                {
                    if (eyes + 2 >= 1280)
                        goto fallback_to_raw;

                    bytes[eyes + 0] = blockMap[current];
                    bytes[eyes + 1] = 1;

                    eyes += 2;
                    previous = current;
                }
                else // increment pair
                {
                    bytes[eyes - 1]++;
                }
            }

            return bytes[..eyes];

        fallback_to_raw:
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    byte[] arr = blocks[x, y]?.Serialize() ?? throw new NullReferenceException("Array elements cannot be null!");
                    Buffer.BlockCopy(arr, 0, bytes, (16 * x + y) * 5, 5);
                }

            return bytes;
        }

        public static BlockData2[,] DeserializeChunk(byte[] bytes, int offset = 0)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length - offset < 1) throw new ArgumentException("Bytes array must be at least 1 byte!");

            BlockData2[,] blocks = new BlockData2[16, 16];
            if (bytes.Length - offset < 1280) // compressed
            {
                Dictionary<byte, BlockData2> blockMap = new();

                int entries = bytes[offset + 0];
                if (entries == 0) entries = 256;

                if (bytes.Length - offset < 1 + entries * 5)
                    entries = 0; // wrong entries, ignore all

                // make dictionary
                int eyes = 1;
                byte ind1 = 0;
                while (entries > 0)
                {
                    blockMap.Add(ind1++, BlockData2.Deserialize(bytes, offset + eyes));
                    eyes += 5;
                    entries--;
                }

                // make array
                int ind2 = 0;
                while (ind2 < 256)
                {
                    BlockData2 block;
                    int count;

                    int remaining = 256 - ind2;

                    if (offset + eyes + 1 < bytes.Length)
                    {
                        blockMap.TryGetValue(bytes[offset + eyes], out block);
                        count = bytes[offset + eyes + 1];
                        if (count == 0) count = 256;

                        if (count > remaining)
                            count = remaining;
                    }
                    else
                    {
                        block = null;
                        count = remaining;
                    }

                    while (count-- > 0)
                    {
                        blocks[(ind2 / 16) % 16, ind2 % 16] = block?.DeepCopy() ?? new BlockData2();
                        ind2++;
                    }

                    eyes += 2;
                }

                return blocks;
            }
            else // non-compressed
            {
                for (int x = 0; x < 16; x++)
                    for (int y = 0; y < 16; y++)
                    {
                        blocks[x, y] = BlockData2.Deserialize(bytes, offset + (16 * x + y) * 5);
                    }

                return blocks;
            }
        }
    }
}
