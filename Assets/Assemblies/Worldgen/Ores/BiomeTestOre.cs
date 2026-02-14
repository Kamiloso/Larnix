
using System.Collections.Generic;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Worldgen.Noise;

namespace Larnix.Worldgen.Ores
{
    public sealed class BiomeTestOre : Ore
    {
        public BiomeTestOre(long seed) : base(seed)
        {
            Seed baseSeed = new Seed(BaseSeed);

            DepthMin = -15;
            DepthMax = int.MinValue;
            OreClusterSizeCutoff = 0.7;
            VariantBased = false;
            BlockByBiome = new Dictionary<BiomeID, BlockData1> {
                { BiomeID.Plains, new(BlockID.Glass,0)},
                { BiomeID.Desert, new(BlockID.Planks,0)}
            };
            
            OreProvider = ValueProvider.CreatePerlin(
                new Perlin(seed: (int)baseSeed.Hash("biome_test_ore"))
                {
                    Octaves = 2,
                    Frequency = 0.1,
                    Lacunarity = 2.0,
                    Persistence = 0.3,
                },
                min: -1, max: 1, dim: 2).Stretch(0.25, 0.25);
        }
    }
}
