#nullable enable
using Larnix.Core.Serialization;
using Larnix.Model.Blocks.Structs;
using System;
using System.Collections.Generic;

namespace Larnix.Model.Blocks.Chunks;

internal static class ChunkSerializer
{
    private const int C_5 = 5; // size of double block header
    private const int C_16 = 16; // size of chunk side
    private const int C_256 = C_16 * C_16; // number of blocks in chunk
    private const int C_1280 = C_5 * C_256; // uncompressed size of chunk

    public static byte[] Serialize(Func<int, int, BlockHeader2> get)
    {
        BlockHeader2 BlockAt(byte b) => get.Invoke(b / C_16, b % C_16);

        var map = new Dictionary<long, byte>();
        byte[] bytes = new byte[C_1280];
        int eyes = 1;

        // Make dictionary
        for (int i = 0; i < C_256; i++)
        {
            BlockHeader2 bh2 = BlockAt((byte)i);
            long unique = UniqueLong(bh2);

            if (map.TryAdd(unique, bytes[0]))
            {
                if (eyes + C_5 >= C_1280)
                    goto fallback_to_raw;

                byte[] blockBytes = Binary<BlockHeader2>.Serialize(bh2);
                Buffer.BlockCopy(blockBytes, 0, bytes, eyes, C_5);
                eyes += C_5;

                unchecked { bytes[0]++; } // can overflow to 256 = 0
            }
        }

        // Fill with data
        long? previous = null;
        for (int i = 0; i < C_256; i++)
        {
            BlockHeader2 bh2 = BlockAt((byte)i);
            long current = UniqueLong(bh2);

            if (previous != current) // next pair
            {
                if (eyes + 2 >= C_1280)
                    goto fallback_to_raw;

                bytes[eyes + 0] = map[current];
                bytes[eyes + 1] = 1;

                eyes += 2;
                previous = current;
            }
            else // increment pair
            {
                unchecked { bytes[eyes - 1]++; }
            }
        }

        return bytes[..eyes];

    fallback_to_raw:
        ChunkIterator.Iterate((x, y) =>
        {
            BlockHeader2 bh2 = get.Invoke(x, y);
            byte[] arr = Binary<BlockHeader2>.Serialize(bh2);
            Buffer.BlockCopy(arr, 0, bytes, (C_16 * x + y) * C_5, C_5);
        },
        IterationOrder.XY);

        return bytes;
    }

    public static void Deserialize(byte[] bytes, Action<int, int, BlockHeader2> set)
    {
        static int Index(int x) => x == 0 ? C_256 : x;

        if (bytes.Length >= C_1280) // non-compressed format
        {
            ChunkIterator.Iterate((x, y) =>
            {
                int blockOffset = (C_16 * x + y) * C_5;
                BlockHeader2 header = Binary<BlockHeader2>.Deserialize(bytes, blockOffset);
                set.Invoke(x, y, header);
            },
            IterationOrder.XY);
        }
        else // compressed format
        {
            Span<BlockHeader2> map = stackalloc BlockHeader2[C_256];
            int entries = bytes.Length > 0 ? Index(bytes[0]) : 0;

            if (bytes.Length < 1 + C_5 * entries)
            {
                entries = 0; // wrong entries, ignore all
            }

            // Form dictionary
            int eyes = 1;
            byte ind1 = 0;
            while (entries > 0)
            {
                BlockHeader2 bh2 = Binary<BlockHeader2>.Deserialize(bytes, eyes);
                map[ind1] = bh2;

                unchecked { ind1++; } // can overflow to 256 = 0

                eyes += C_5;
                entries--;
            }

            // Make array
            int ind2 = 0;
            while (ind2 < C_256)
            {
                int count;
                BlockHeader2 block;

                int remaining = C_256 - ind2;

                if (eyes + 1 < bytes.Length)
                {
                    byte id = bytes[eyes];
                    block = map[id];

                    count = Index(bytes[eyes + 1]);
                    count = Math.Min(count, remaining);
                }
                else
                {
                    block = BlockHeader2.Empty;
                    count = remaining;
                }

                // this operator really EXISTS!
                // "while count approaches 0": ... 3, 2, 1, 0 (stop)
                while (count --> 0)
                {
                    int x = ind2 / C_16;
                    int y = ind2 % C_16;

                    set.Invoke(x, y, block);
                    ind2++;
                }

                eyes += 2;
            }
        }
    }

    private static long UniqueLong(in BlockHeader2 bh2)
    {
        long fid = (long)bh2.Front.Id & 0xFFFF;
        long fvr = (long)bh2.Front.Variant & 0xFF;
        long bid = (long)bh2.Back.Id & 0xFFFF;
        long bvr = (long)bh2.Back.Variant & 0xFF;

        long front = fid | fvr << 16;
        long back = bid | bvr << 16;

        return front | back << 24;
    }
}
