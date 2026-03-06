using Larnix.Blocks;
using Larnix.Worldgen.Noise;
using Larnix.Blocks.Structs;

namespace Larnix.Worldgen.Ores.All
{
    internal sealed class TestOre : Ore
    {
        public override BlockData1 DefaultBlock => new(BlockID.Plastic, 0);
        public override double OreClusterSizeCutoff => 0.5;
        public override int MaxHeight => -10;
        public override int MinHeight => -100;

        public TestOre(Seed seed) : base(seed)
        {
            OreProvider = ValueProvider.CreatePerlin(
                new Perlin(seed: (int)Seed.Hash("test_ore"))
                {
                    Octaves = 2,
                    Frequency = 0.1,
                    Lacunarity = 2.0,
                    Persistence = 0.3,
                },
                min: -1, max: 1, dim: 2).Stretch(0.75, 0.25);
        }
    }
}
