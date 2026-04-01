using System;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.GameCore.Structs;
using Larnix.GameCore.Utils;

namespace Larnix.Worldgen.Transformers.Pipeline;

public class ApplyRealBlocks : Transformer<BlockHeader2, BlockData2>
{
    public ApplyRealBlocks(UsefulBag usefulBag) : base(usefulBag)
    {
        ;
    }

    public override BlockData2[,] Rebuild(Vec2Int chunk, BlockHeader2[,] chunkIn)
    {
        BlockData2[,] result = ChunkIterator.Array2D<BlockData2>();

        ChunkIterator.Iterate((x, y) =>
        {
            result[x, y] = new BlockData2(chunkIn[x, y]);
        });

        return result;
    }
}
