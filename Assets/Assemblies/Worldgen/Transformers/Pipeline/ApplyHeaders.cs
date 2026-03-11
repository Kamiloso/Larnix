using System;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.GameCore.Utils;
using Larnix.Core.Vectors;
using Larnix.Worldgen.Biomes;
using Larnix.GameCore.Structs;

namespace Larnix.Worldgen.Transformers.Pipeline
{
    public class ApplyHeaders : Transformer<ProtoBlock, BlockHeader2>
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
}
