using System;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Worldgen.Biomes;
using Larnix.Worldgen.Biomes.All;
using Larnix.Worldgen.Ores;

namespace Larnix.Worldgen.Transformers.Pipeline
{
    public class AddOreClusters : Transformer<BlockData2, BlockData2>
    {
        public AddOreClusters(UsefulBag usefulBag) : base(usefulBag)
        {
            ;
        }

        public override BlockData2[,] Rebuild(Vec2Int chunk, BlockData2[,] blocks)
        {
            ChunkIterator.IterateWithPOS(chunk, (POS, x, y) =>
            {
                Biome biome = Generator.BiomeObjectAt(POS);

                if (biome is not IHasOre iface)
                    return; // no ores in this biome
                
                foreach (Ore ore in iface.ORES())
                {
                    BlockData1 newBlock;

                    if (ore.FrontEnabled && // front layer ores
                        ore.TryGenerateOre(POS, blocks[x, y].Front, out newBlock))
                    {
                        blocks[x, y] = new BlockData2(newBlock, blocks[x, y].Back);
                    }

                    if (ore.BackEnabled && // back layer ores
                        ore.TryGenerateOre(POS, blocks[x, y].Back, out newBlock))
                    {
                        blocks[x, y] = new BlockData2(blocks[x, y].Front, newBlock);
                    }
                }
            });
            
            return blocks;
        }
    }
}
