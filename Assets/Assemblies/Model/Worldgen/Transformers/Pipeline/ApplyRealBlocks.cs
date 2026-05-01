using System;
using Larnix.Model.Blocks;
using Larnix.Model.Blocks.Structs;
using Larnix.Core.Vectors;

namespace Larnix.Model.Worldgen.Transformers.Pipeline;

internal class ApplyRealBlocks : Transformer<BlockHeader2, BlockData2>
{
    public ApplyRealBlocks(UsefulBag usefulBag) : base(usefulBag)
    {
        ;
    }

    public override BlockData2[,] Rebuild(Vec2Int chunk, BlockHeader2[,] chunkIn)
    {
        BlockData2[,] result = ChunkIterator.Array16x16<BlockData2>();

        ChunkIterator.Iterate((x, y) =>
        {
            result[x, y] = new BlockData2(chunkIn[x, y]);
        });

        return result;
    }
}
