using Larnix.Core.Vectors;
using Larnix.Model.Worldgen.Biomes;
using Larnix.Model.Worldgen.Biomes.All;
using Larnix.Model.Worldgen.Ores;
using Larnix.Model.Utils;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Model.Worldgen.Transformers.Pipeline;

internal class AddOreClusters : Transformer<BlockHeader2, BlockHeader2>
{
    public AddOreClusters(UsefulBag usefulBag) : base(usefulBag)
    {
        ;
    }

    public override BlockHeader2[,] Rebuild(Vec2Int chunk, BlockHeader2[,] blocks)
    {
        ChunkIterator.IterateWithPOS(chunk, (POS, x, y) =>
        {
            Biome biome = Generator.BiomeObjectAt(POS);

            if (biome is not IHasOre iface)
                return; // no ores in this biome

            foreach (Ore ore in iface.ORES())
            {
                BlockHeader1 newBlock;

                if (ore.FrontEnabled && // front layer ores
                    ore.TryGenerateOre(POS, blocks[x, y].Front, out newBlock))
                {
                    blocks[x, y] = new BlockHeader2(newBlock, blocks[x, y].Back);
                }

                if (ore.BackEnabled && // back layer ores
                    ore.TryGenerateOre(POS, blocks[x, y].Back, out newBlock))
                {
                    blocks[x, y] = new BlockHeader2(blocks[x, y].Front, newBlock);
                }
            }
        });

        return blocks;
    }
}
