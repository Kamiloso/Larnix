using System;
using Larnix.Core.Vectors;
using Larnix.Model.Worldgen.Noise;
using Larnix.Model.Worldgen.Providers;
using Larnix.Model.Utils;

namespace Larnix.Model.Worldgen.Transformers.Pipeline;

internal class BuildBaseTerrain : Transformer<object, ProtoBlock>
{
    const int WATER_LEVEL = -1;
    const int SOIL_LAYER_SIZE = 3;

    public BuildBaseTerrain(UsefulBag usefulBag) : base(usefulBag)
    {
        var surfaceNoise = ValueProvider.CreatePerlin(
            new Perlin(seed: Seed.HashInt("noise_surface"))
            {
                Octaves = 4,
                Frequency = 0.013,
                Lacunarity = 2.0,
                Persistence = 0.3,
            },
            min: -25.0, max: 40.0, dim: 1);

        Providers["SURFACE"] = ValueProvider.CreateFunction((x, y, z) =>
        {
            double val = surfaceNoise.GetValue(x, y, z);
            return val > 0.0 ? val : val * 2.0 / 3.0;
        });

        Providers["RELATIVE_HEIGHT"] = ValueProvider.CreateFunction((x, y, z) =>
        {
            return y - Providers["SURFACE"].GetValue(x);
        });
    }

    public override ProtoBlock[,] Rebuild(Vec2Int chunk, object[,] _)
    {
        ProtoBlock[,] blocks = ChunkIterator.Array2D<ProtoBlock>();

        ChunkIterator.IterateWithPOS(chunk, (POS, x, y) =>
        {
            int surfaceLevel = (int)Math.Floor(Providers["SURFACE"].GetValue(POS.x));
            int stoneLevel = surfaceLevel - SOIL_LAYER_SIZE;

            blocks[x, y] = POS.y switch
            {
                var _ when POS.y > surfaceLevel => POS.y <= WATER_LEVEL
                    ? ProtoBlock.Lake
                    : ProtoBlock.Sky,

                var _ when POS.y > stoneLevel => POS.y == surfaceLevel
                    ? ProtoBlock.Surface
                    : ProtoBlock.Dirt,

                _ => ProtoBlock.Stone
            };
        });

        return blocks;
    }
}
