using Larnix.Worldgen.Noise;
using Larnix.Blocks.Structs;
using Larnix.Blocks;

namespace Larnix.Worldgen.Ores.All
{
    internal sealed class BiomeTestOre : Ore
    {
        public override BlockData1 DefaultBlock => new(BlockID.Bedrock, 0);
        public override double OreClusterSizeCutoff => 0.7;
        public override int MaxHeight => -15;
        public override int MinHeight => int.MinValue;

        public BiomeTestOre(Seed seed) : base(seed)
        {   
            OreProvider = ValueProvider.CreatePerlin(
                new Perlin(seed: (int)Seed.Hash("biome_test_ore"))
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
