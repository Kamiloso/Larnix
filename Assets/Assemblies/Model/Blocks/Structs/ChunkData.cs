#nullable enable
using System;
using SimpleJSON;
using System.Collections.Generic;
using Larnix.Model.Json;
using Larnix.Model.Utils;
using Larnix.Core.Serialization;

namespace Larnix.Model.Blocks.Structs;

public class ChunkData
{
    private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;
    private static readonly int S = Binary<BlockHeader2>.Size; // S = 5

    public BlockData2[,] Blocks { get; }

    private ChunkView? _headerLook;
    public ChunkView HeaderView => _headerLook ??= new ChunkView(this);

    public BlockData2 this[int x, int y]
    {
        get => Blocks[x, y];
        set => Blocks[x, y] = value ?? BlockData2.Empty;
    }

    public ChunkData()
    {
        Blocks = ChunkIterator.Array2D<BlockData2>();
        FillWith(() => BlockData2.Empty);
    }

    public void FillWith(Func<BlockData2> factory)
    {
        ChunkIterator.Iterate((x, y) => Blocks[x, y] = factory());
    }

    public byte[] Serialize()
    {
        BlockHeader2 BlockIndexer(byte b) =>
            Blocks[b / CHUNK_SIZE, b % CHUNK_SIZE].Header;

        Dictionary<long, byte> blockMap = new();
        byte[] bytes = new byte[1280];
        int eyes = 1;

        // make dictionary
        for (int i = 0; i < 256; i++)
        {
            BlockHeader2 bh2 = BlockIndexer((byte)i);
            if (blockMap.TryAdd(UniqueLong(bh2), bytes[0]))
            {
                if (eyes + S >= 1280)
                    goto fallback_to_raw;

                byte[] blockBytes = Binary<BlockHeader2>.Serialize(bh2);
                Buffer.BlockCopy(blockBytes, 0, bytes, eyes, S);
                eyes += S;
                bytes[0]++; // can overflow to 256 = 0
            }
        }

        // fill with data
        long? previous = null;
        for (int i = 0; i < 256; i++)
        {
            BlockHeader2 bh2 = BlockIndexer((byte)i);
            long current = UniqueLong(bh2);

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
        ChunkIterator.Iterate((x, y) =>
        {
            byte[] arr = Binary<BlockHeader2>.Serialize(Blocks[x, y].Header);
            Buffer.BlockCopy(arr, 0, bytes, (CHUNK_SIZE * x + y) * S, S);

        }, IterationOrder.XY);

        return bytes;
    }

    public void DeserializeInPlace(byte[] bytes, int offset = 0)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));

        if (bytes.Length - offset < 1)
        {
            FillWith(() => BlockData2.Empty);
            return;
        }

        if (bytes.Length - offset < 1280) // compressed
        {
            Dictionary<byte, BlockHeader2> blockMap = new();

            int entries = bytes[offset + 0];
            if (entries == 0) entries = 256;

            if (bytes.Length - offset < 1 + entries * S)
                entries = 0; // wrong entries, ignore all

            // make dictionary
            int eyes = 1;
            byte ind1 = 0;
            while (entries > 0)
            {
                BlockHeader2 bh2 = Binary<BlockHeader2>.Deserialize(bytes, offset + eyes);
                blockMap.Add(ind1++, bh2);

                eyes += S;
                entries--;
            }

            // make array
            int ind2 = 0;
            while (ind2 < 256)
            {
                BlockHeader2 block;
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
                    block = BlockHeader2.Empty;
                    count = remaining;
                }

                while (count --> 0)
                {
                    int x = ind2 / CHUNK_SIZE % CHUNK_SIZE;
                    int y = ind2 % CHUNK_SIZE;

                    Blocks[x, y] = new BlockData2(block);
                    ind2++;
                }

                eyes += 2;
            }
        }
        else // non-compressed
        {
            ChunkIterator.Iterate((x, y) =>
            {
                int realOffset = offset + (CHUNK_SIZE * x + y) * S;
                BlockHeader2 header = Binary<BlockHeader2>.Deserialize(bytes, realOffset);
                Blocks[x, y] = new BlockData2(header);

            }, IterationOrder.XY);
        }
    }

    public static ChunkData Deserialize(byte[] bytes, int offset = 0)
    {
        ChunkData chunkData = new();
        chunkData.DeserializeInPlace(bytes, offset);
        return chunkData;
    }

    public void ImportData(string? chunkJson)
    {
        JSONObject root = JsonUtils.ToJsonObject(chunkJson);

        for (int x = 0; x < CHUNK_SIZE; x++)
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                string key;
                Storage? s1 = null, s2 = null;

                key = "F_" + x + "_" + y;
                if (root[key] is JSONString node1)
                    s1 = Storage.FromString(node1.Value);

                key = "B_" + x + "_" + y;
                if (root[key] is JSONString node2)
                    s2 = Storage.FromString(node2.Value);

                BlockData2 old = Blocks[x, y];
                Blocks[x, y] = new BlockData2(
                    new(old.Front.ID, old.Front.Variant, s1 ?? new()),
                    new(old.Back.ID, old.Back.Variant, s2 ?? new())
                );
            }
    }

    public string ExportData()
    {
        JSONObject root = new();
        for (int x = 0; x < CHUNK_SIZE; x++)
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                string key, value;

                key = $"F_{x}_{y}";
                if ((value = Blocks[x, y].Front.NBT.ToString()) != "{}")
                    root[key] = new JSONString(value);

                key = $"B_{x}_{y}";
                if ((value = Blocks[x, y].Back.NBT.ToString()) != "{}")
                    root[key] = new JSONString(value);
            }

        return root.ToString();
    }

    private static long UniqueLong(BlockHeader2 bh2)
    {
        long frontPart = ((long)bh2.Front.Id & 0xFFFF) | (((long)bh2.Front.Variant & 0xFF) << 16);
        long backPart = ((long)bh2.Back.Id & 0xFFFF) | (((long)bh2.Back.Variant & 0xFF) << 16);

        return frontPart | (backPart << 24);
    }
}

#region Chunk Headers View

public class ChunkView
{
    private readonly ChunkData _chunk;

    public BlockHeader2 this[int x, int y]
    {
        get => _chunk.Blocks[x, y].Header;
        set => _chunk.Blocks[x, y] = new BlockData2(value);
    }

    public ChunkView(ChunkData chunkData)
    {
        _chunk = chunkData;
    }

    public byte[] Serialize()
    {
        return _chunk.Serialize();
    }

    public static ChunkView Deserialize(byte[] bytes, int offset = 0)
    {
        return new ChunkView(
            ChunkData.Deserialize(bytes, offset)
        );
    }
}

#endregion
