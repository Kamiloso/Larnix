
using Larnix.Blocks;
using Larnix.Worldgen.Noise;

namespace Larnix.Worldgen.Ores
{
    public sealed class TestOre : Ore
    {
        public TestOre(long seed) : base(seed)
        {
            Seed baseSeed = new Seed(BaseSeed);

            DepthMin = -10;
            DepthMax = -20;
            OreClusterSizeCutoff = 0.5;
            OreBlockId = BlockID.Plastic;
            OreProvider = ValueProvider.CreatePerlin(
                new Perlin(seed: (int)baseSeed.Hash("test_ore"))
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
