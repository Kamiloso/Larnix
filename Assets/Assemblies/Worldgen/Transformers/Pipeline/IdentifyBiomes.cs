using System;
using Larnix.Core.Vectors;
using Larnix.Worldgen.Noise;
using Larnix.Worldgen.Providers;

namespace Larnix.Worldgen.Transformers.Pipeline;

public class IdentifyBiomes : Transformer<object, object>
{
    public IdentifyBiomes(UsefulBag usefulBag) : base(usefulBag)
    {
        Providers["TEMPERATURE"] = ValueProvider.CreatePerlin(
            new Perlin(seed: Seed.HashInt("noise_temperature"))
            {
                Octaves = 1,
                Frequency = 0.0015,
                Lacunarity = 1.8,
                Persistence = 0.4,
            },
            min: -1.0, max: 1.0, dim: 2);

        Providers["HUMIDITY"] = ValueProvider.CreatePerlin(
            new Perlin(seed: Seed.HashInt("noise_humidity"))
            {
                Octaves = 1,
                Frequency = 0.0015,
                Lacunarity = 1.8,
                Persistence = 0.4,
            },
            min: -1.0, max: 1.0, dim: 2);
    }

    public override object[,] Rebuild(Vec2Int chunk, object[,] x) => x;
}
