#nullable enable
using Larnix.Model.Utils;
using Larnix.Model.Blocks.Structs;
using System;

namespace Larnix.Model.Blocks.Chunks;

public class ChunkData
{
    private readonly BlockData2[,] _blocks = ChunkIterator.Array16x16<BlockData2>();

    public BlockData2 this[int x, int y]
    {
        get => _blocks[x, y];
        set => _blocks[x, y] = value ?? throw new ArgumentNullException(nameof(value));
    }

    public ChunkData(Func<int, int, BlockData2> initializer)
    {
        ChunkIterator.Iterate((x, y) =>
        {
            _blocks[x, y] = initializer.Invoke(x, y);
        });
    }

    public ChunkData(byte[] bytes, string? chunkJson = null)
    {
        ChunkSerializer.Deserialize(bytes, (x, y, bh2) =>
        {
            _blocks[x, y] = new BlockData2(
                new BlockData1(bh2.Front),
                new BlockData1(bh2.Back)
                );
        });
        ChunkNbtImporter.ImportData(this, chunkJson);
    }

    public byte[] Serialize(out string chunkJson)
    {
        chunkJson = ChunkNbtImporter.ExportData(this);
        return ChunkSerializer.Serialize((x, y) => _blocks[x, y].Header);
    }
}
