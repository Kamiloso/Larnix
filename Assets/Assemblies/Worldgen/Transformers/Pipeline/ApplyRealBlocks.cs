using System;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.GameCore.Utils;
using Larnix.Core.Vectors;
using Larnix.Worldgen.Biomes;

namespace Larnix.Worldgen.Transformers.Pipeline
{
    public class ApplyRealBlocks : Transformer<ProtoBlock, BlockData2>
    {
        public ApplyRealBlocks(UsefulBag usefulBag) : base(usefulBag)
        {
            ;
        }

        public override BlockData2[,] Rebuild(Vec2Int chunk, ProtoBlock[,] chunkIn)
        {
            BlockData2[,] blocks = ChunkIterator.Array2D<BlockData2>();

            ChunkIterator.IterateWithPOS(chunk, (POS, x, y) =>
            {
                Vec2 position = BlockUtils.BlockCenter(POS);
                Biome biome = Generator.Biomes[Generator.BiomeAt(position)];

                blocks[x, y] = biome.TranslateProtoBlock(chunkIn[x, y]);
            });
            
            return blocks;
        }
    }
}
