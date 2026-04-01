using System;
using Larnix.Model.Blocks;
using Larnix.Model.Blocks.Structs;
using Larnix.Model.Utils;
using Larnix.Core.Vectors;
using Larnix.Model.Worldgen.Biomes;

namespace Larnix.Model.Worldgen.Transformers.Pipeline;

internal class ApplyHeaders : Transformer<ProtoBlock, BlockHeader2>
{
    public ApplyHeaders(UsefulBag usefulBag) : base(usefulBag)
    {
        ;
    }

    public override BlockHeader2[,] Rebuild(Vec2Int chunk, ProtoBlock[,] chunkIn)
    {
        BlockHeader2[,] blocks = ChunkIterator.Array2D<BlockHeader2>();

        ChunkIterator.IterateWithPOS(chunk, (POS, x, y) =>
        {
            Vec2 position = BlockUtils.BlockCenter(POS);
            Biome biome = Generator.Biomes[Generator.BiomeAt(position)];

            blocks[x, y] = biome.TranslateProtoBlock(chunkIn[x, y]);
        });

        return blocks;
    }
}
