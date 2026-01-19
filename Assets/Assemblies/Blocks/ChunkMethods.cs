using System;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public static class ChunkMethods
    {
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
