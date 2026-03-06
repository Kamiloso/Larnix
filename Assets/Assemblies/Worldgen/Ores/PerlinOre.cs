using Larnix.Core.Vectors;
using Larnix.Worldgen.Noise;

namespace Larnix.Worldgen.Ores
{
    public class PerlinOre : Ore
    {
        private readonly Seed _seed;
        private readonly string _saltPhrase;

        private ValueProvider _oreProvider;
        private ValueProvider OreProvider
        {
            get => _oreProvider ??= CreateProvider();
            set => _oreProvider = value;
        }

        // --- NOISE SETTINGS ---
        public int Octaves { get; init; } = 3;
        public double Frequency { get; init; } = 0.1;
        public double Lacunarity { get; init; } = 2.0;
        public double Persistence { get; init; } = 0.3;

        // --- NOISE MODIFIERS ---
        public double StretchX { get; init; } = 1.0;
        public double StretchY { get; init; } = 0.5;

        // --- ORE CONDITIONS ---
        public double MinNoise { get; init; } = 0.75;
        public double MaxNoise { get; init; } = double.MaxValue;
        public double MinHeight { get; init; } = double.MinValue;
        public double MaxHeight { get; init; } = double.MaxValue;
        public double TransitionWidth { get; init; } = 30.0;

        public PerlinOre(Seed seed, string saltPhrase)
        {
            _seed = seed;
            _saltPhrase = saltPhrase;
            _oreProvider = null;
        }

        private ValueProvider CreateProvider()
        {
            var height = ValueProvider.CreateFunction(
                (x, y, _) => y);
            
            var condition = ValueProvider.CreateCondition(
                height, MinHeight, MaxHeight, TransitionWidth);

            var provider = ValueProvider.CreatePerlin(
                new Perlin(seed: (int)_seed.Hash(_saltPhrase))
                {
                    Octaves = Octaves,
                    Frequency = Frequency,
                    Lacunarity = Lacunarity,
                    Persistence = Persistence,
                },
                    min: 0.0,
                    max: 1.0,
                    dim: 2
                )
                .Stretch(StretchX, StretchY)
                .If(condition);

            return provider;
        }

        public override bool OrePresentAt(Vec2Int POS)
        {
            double value = OreProvider.GetValue(POS.x, POS.y);
            return value >= MinNoise && value <= MaxNoise;
        }
    }
}
