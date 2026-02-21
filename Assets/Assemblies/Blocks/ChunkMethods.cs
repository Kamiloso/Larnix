using System;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Core.Binary;
using SimpleJSON;
using Larnix.Core.Json;
using Larnix.Core.Utils;

namespace Larnix.Blocks
{
    public static class ChunkMethods
    {
        public const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;

        public static void InsertData(this BlockData2[,] chunk, string chunkJson)
        {
            HandleInputErrors(chunk);

            JSONObject root;
            
            try
            {
                root = !string.IsNullOrEmpty(chunkJson) ?
                    (JSON.Parse(chunkJson).AsObject ?? new()) : new();
            }
            catch
            {
                root = new();
            }
             
            for (int x = 0; x < CHUNK_SIZE; x++)
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    string key;
                    Storage s1 = null, s2 = null;

                    key = "F_" + x + "_" + y;
                    if (root[key] is JSONString node1)
                        s1 = Storage.FromString(node1.Value);

                    key = "B_" + x + "_" + y;
                    if (root[key] is JSONString node2)
                        s2 = Storage.FromString(node2.Value);
                    
                    BlockData2 old = chunk[x, y];
                    chunk[x, y] = new BlockData2(
                        new(old.Front.ID, old.Front.Variant, s1 ?? new()),
                        new(old.Back.ID, old.Back.Variant, s2 ?? new())
                    );
                }
        }

        public static string ExportData(this BlockData2[,] chunk)
        {
            HandleInputErrors(chunk);

            JSONObject root = new();
            for (int x = 0; x < CHUNK_SIZE; x++)
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    string key, value;

                    key = "F_" + x + "_" + y;
                    if ((value = chunk[x, y].Front.Data.ToString()) != "{}")
                        root[key] = new JSONString(value);

                    key = "B_" + x + "_" + y;
                    if ((value = chunk[x, y].Back.Data.ToString()) != "{}")
                        root[key] = new JSONString(value);
                }

            return root.ToString();
        }

        public static BlockData2[,] DeepCopyChunk(this BlockData2[,] original)
        {
            HandleInputErrors(original);

            BlockData2[,] copy = ChunkIterator.Array2D<BlockData2>();
            for (int x = 0; x < CHUNK_SIZE; x++)
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    copy[x, y] = original[x, y].DeepCopy();
                }

            return copy;
        }

        public static byte[] SerializeChunk(this BlockData2[,] blocks)
        {
            HandleInputErrors(blocks);
            
            Func<byte, BlockData2> BlockIndexer = b => blocks[b / CHUNK_SIZE, b % CHUNK_SIZE];

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

                    byte[] blockBytes = Structures.GetBytes(bd2);
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
            for (int x = 0; x < CHUNK_SIZE; x++)
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    byte[] arr = Structures.GetBytes(blocks[x, y]);
                    Buffer.BlockCopy(arr, 0, bytes, (CHUNK_SIZE * x + y) * 5, 5);
                }

            return bytes;
        }

        public static BlockData2[,] DeserializeChunk(byte[] bytes, int offset = 0)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length - offset < 1) throw new ArgumentException("Bytes array must be at least 1 byte!");

            BlockData2[,] blocks = ChunkIterator.Array2D<BlockData2>();
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
                    blockMap.Add(ind1++, Structures.FromBytes<BlockData2>(bytes, offset + eyes));
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
                        blocks[(ind2 / CHUNK_SIZE) % CHUNK_SIZE, ind2 % CHUNK_SIZE] = block?.DeepCopy() ?? new BlockData2();
                        ind2++;
                    }

                    eyes += 2;
                }

                return blocks;
            }
            else // non-compressed
            {
                for (int x = 0; x < CHUNK_SIZE; x++)
                    for (int y = 0; y < CHUNK_SIZE; y++)
                    {
                        blocks[x, y] = Structures.FromBytes<BlockData2>(bytes, offset + (CHUNK_SIZE * x + y) * 5);
                    }

                return blocks;
            }
        }

        private static void HandleInputErrors(BlockData2[,] chunk)
        {
            if (chunk == null) throw new ArgumentNullException("Chunk array cannot be null.");
            if (chunk.GetLength(0) != CHUNK_SIZE || chunk.GetLength(1) != CHUNK_SIZE)
            {
                throw new ArgumentException($"Blocks array must be {CHUNK_SIZE} x {CHUNK_SIZE}.");
            }
        }
    }
}
