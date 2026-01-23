using System;

namespace Larnix.Worldgen.Noise
{
    public class Perlin
    {
        private readonly int[] perm = new int[512];

        public int Octaves { get; set; } = 4;
        public double Frequency { get; set; } = 0.01;
        public double Amplitude { get; set; } = 1.0;
        public double Lacunarity { get; set; } = 2.0;
        public double Persistence { get; set; } = 0.4;

        public Perlin(int seed)
        {
            var rng = new Random(seed);
            int[] p = new int[256];

            for (int i = 0; i < 256; i++)
                p[i] = i;

            for (int i = 255; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }

            for (int i = 0; i < 512; i++)
                perm[i] = p[i & 255];
        }

        public double Sample(double x, double y, double z)
        {
            double value = 0f;
            double freq = Frequency;
            double amp = Amplitude;

            for (int i = 0; i < Octaves; i++)
            {
                value += Single(x * freq, y * freq, z * freq) * amp;

                freq *= Lacunarity;
                amp *= Persistence;
            }

            return value;
        }

        private double Single(double x, double y, double z)
        {
            int X = FastFloor(x) & 255;
            int Y = FastFloor(y) & 255;
            int Z = FastFloor(z) & 255;

            x -= FastFloor(x);
            y -= FastFloor(y);
            z -= FastFloor(z);

            double u = Fade(x);
            double v = Fade(y);
            double w = Fade(z);

            int A = perm[X] + Y;
            int AA = perm[A] + Z;
            int AB = perm[A + 1] + Z;
            int B = perm[X + 1] + Y;
            int BA = perm[B] + Z;
            int BB = perm[B + 1] + Z;

            return Lerp(w,
                Lerp(v,
                    Lerp(u, Grad(perm[AA], x, y, z),
                            Grad(perm[BA], x - 1, y, z)),
                    Lerp(u, Grad(perm[AB], x, y - 1, z),
                            Grad(perm[BB], x - 1, y - 1, z))),
                Lerp(v,
                    Lerp(u, Grad(perm[AA + 1], x, y, z - 1),
                            Grad(perm[BA + 1], x - 1, y, z - 1)),
                    Lerp(u, Grad(perm[AB + 1], x, y - 1, z - 1),
                            Grad(perm[BB + 1], x - 1, y - 1, z - 1)))
            );
        }

        private static int FastFloor(double x) =>
            x >= 0 ? (int)x : (int)x - 1;

        private static double Fade(double t) =>
            t * t * t * (t * (t * 6 - 15) + 10);

        private static double Lerp(double t, double a, double b) =>
            a + t * (b - a);

        private static double Grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;
            double u = h < 8 ? x : y;
            double v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) +
                ((h & 2) == 0 ? v : -v);
        }
    }
}
