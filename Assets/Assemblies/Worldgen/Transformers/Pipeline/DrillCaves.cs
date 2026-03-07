using System;
using Larnix.Blocks;
using Larnix.Core.Vectors;
using Larnix.Worldgen.Noise;
using Larnix.Worldgen.Providers;

namespace Larnix.Worldgen.Transformers.Pipeline
{
    public class DrillCaves : Transformer<ProtoBlock, ProtoBlock>
    {
        const double CAVE_NOISE_MIN = 0.4;
        const double CAVE_NOISE_MAX = 0.6;

        public DrillCaves(UsefulBag usefulBag) : base(usefulBag)
        {
            var caveNoise = ValueProvider.CreatePerlin(
                new Perlin(seed: (int)Seed.Hash("noise_cave"))
                {
                    Octaves = 3,
                    Frequency = 0.025,
                    Lacunarity = 1.8,
                    Persistence = 0.3,
                },
                min: 0.0, max: 1.0, dim: 2).Stretch(1.25, 0.75);

            BoolProvider isDrySpace = Providers["SURFACE"].Over(2.0, 12.0);
            BoolProvider isUnderground = Providers["RELATIVE_HEIGHT"].Under(-4.0, -34.0);

            Providers["CAVE"] = caveNoise.When(isDrySpace | isUnderground);
        }

        public override ProtoBlock[,] Rebuild(Vec2Int chunk, ProtoBlock[,] blocks)
        {
            ChunkIterator.IterateWithPOS(chunk, (POS, x, y) =>
            {
                double caveValue = Providers["CAVE"].GetValue(POS.x, POS.y);
                if (caveValue >= CAVE_NOISE_MIN && caveValue <= CAVE_NOISE_MAX)
                {
                    blocks[x, y] = blocks[x, y] == ProtoBlock.Stone
                        ? ProtoBlock.Cave
                        : ProtoBlock.Sky;
                }
            });

            return blocks;
        }
    }
}
