using System;

namespace Larnix.Worldgen;

internal class Utils
{
    public static T ValueFromGradient<T>(T value1, T value2, double gradient, long entropySource)
    {
        gradient = Math.Clamp(gradient, 0.0, 1.0);

        var rng = new Random((int)(entropySource ^ (entropySource >> 32)));
        double roll = rng.NextDouble();

        return roll < gradient ? value2 : value1;
    }
}
