using System;
using Larnix.Model.Worldgen.Noise;

namespace Larnix.Model.Worldgen.Providers;

internal class BoolProvider : ValueProvider
{
    protected BoolProvider() { }

    private static double SmoothStep(double t) => t * t * (3.0 - 2.0 * t);

    public static BoolProvider FromSlopeUp(ValueProvider baseProvider, double v1, double v2)
    {
        double a = Math.Min(v1, v2);
        double b = Math.Max(v1, v2);

        BoolProvider provider = new();
        provider._fun = (x, y, z) =>
        {
            double v = baseProvider.GetValue(x, y, z);
            if (v <= a) return 0.0;
            if (v >= b) return 1.0;

            double t = (v - a) / (b - a);
            return SmoothStep(t);
        };
        return provider;
    }

    public static BoolProvider FromSlopeDown(ValueProvider baseProvider, double v1, double v2)
    {
        return FromSlopeUp(baseProvider, v1, v2).Negate();
    }

#region Logical Operations

    public BoolProvider Negate()
    {
        BoolProvider provider = new();
        provider._fun = (x, y, z) => 1.0 - GetValue(x, y, z);
        return provider;
    }

    public BoolProvider And(BoolProvider condition)
    {
        BoolProvider provider = new();
        provider._fun = (x, y, z) => Math.Min(GetValue(x, y, z), condition.GetValue(x, y, z));
        return provider;
    }

    public BoolProvider Or(BoolProvider condition)
    {
        BoolProvider provider = new();
        provider._fun = (x, y, z) => Math.Max(GetValue(x, y, z), condition.GetValue(x, y, z));
        return provider;
    }

    public static BoolProvider operator !(BoolProvider provider) => provider.Negate();
    public static BoolProvider operator &(BoolProvider a, BoolProvider b) => a.And(b);
    public static BoolProvider operator |(BoolProvider a, BoolProvider b) => a.Or(b);

#endregion

}
