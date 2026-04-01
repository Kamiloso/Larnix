using System;
using Larnix.Core.Vectors;
using Larnix.Model.Worldgen.Noise;

namespace Larnix.Model.Worldgen.Providers;

internal class ValueProvider
{
    protected Func<double, double, double, double> _fun = (_, _, _) => 0.0;

    protected ValueProvider() { }

    public double GetValue(double x = 0, double y = 0, double z = 0)
    {
        return _fun(x, y, z);
    }

    public double GetValue(Vec2 position)
    {
        return GetValue(position.x, position.y);
    }

    public BoolProvider Over(double v1, double v2)
    {
        double a = Math.Min(v1, v2);
        double b = Math.Max(v1, v2);
        return BoolProvider.FromSlopeUp(this, a, b);
    }

    public BoolProvider Under(double v1, double v2)
    {
        double a = Math.Min(v1, v2);
        double b = Math.Max(v1, v2);
        return BoolProvider.FromSlopeDown(this, a, b);
    }

    public BoolProvider Between(double min, double max,
        double? bufmid = null, double? bufin = null, double? bufout = null)
    {
        int has = (bufmid.HasValue ? 1 : 0) + (bufin.HasValue ? 1 : 0) + (bufout.HasValue ? 1 : 0);
        if (has != 1) throw new ArgumentException("Exactly one of bufmid, bufin, or bufout must be provided!");

        double minL = min, maxL = max;
        double width = bufmid ?? bufin ?? bufout.Value;

        if (bufmid.HasValue)
        {
            minL -= width / 2.0;
            maxL -= width / 2.0;
        }

        if (bufin.HasValue)
        {
            maxL -= width;
        }

        if (bufout.HasValue)
        {
            minL -= width;
        }

        BoolProvider over = Over(minL, minL + width);
        BoolProvider under = Under(maxL, maxL + width);

        return over.And(under);
    }

#region Factory Methods

    public static ValueProvider CreatePerlin(Perlin perlin, double min, double max, int dim)
    {
        if (dim >= 3)
        {
            throw new ArgumentException(
                "Cannot create a 3 or higher dimensional noise provider! " +
                "Third axis must be used to remove zeros.");
        }

        ValueProvider provider = new();
        provider._fun = (x, y, z) => (perlin.Sample(
            dim >= 1 ? x : 0,
            dim >= 2 ? y : 0,
            1.0 / perlin.Frequency / 2.0) + 1.0) / 2.0 * (max - min) + min;
        return provider;
    }

    public static ValueProvider CreateFunction(Func<double, double, double, double> function)
    {
        ValueProvider provider = new();
        provider._fun = (x, y, z) => function(x, y, z);
        return provider;
    }

    public static ValueProvider CreateConstant(double value)
    {
        ValueProvider provider = new();
        provider._fun = (_, _, _) => value;
        return provider;
    }

    public static implicit operator ValueProvider(double value) => CreateConstant(value);

#endregion
#region Manipulation

    public ValueProvider Offset(double off_x, double off_y, double off_z)
    {
        ValueProvider provider = new();
        provider._fun = (x, y, z) => GetValue(x - off_x, y - off_y, z - off_z);
        return provider;
    }

    public ValueProvider Stretch(double stretch_x = 1.0, double stretch_y = 1.0, double stretch_z = 1.0)
    {
        ValueProvider provider = new();
        provider._fun = (x, y, z) => GetValue(x / stretch_x, y / stretch_y, z / stretch_z);
        return provider;
    }

    public ValueProvider When(BoolProvider condition)
    {
        ValueProvider provider = new();
        provider._fun = (x, y, z) => GetValue(x, y, z) * condition.GetValue(x, y, z);
        return provider;
    }

#endregion
#region Arithmetic Operations

    public ValueProvider Add(ValueProvider other)
    {
        ValueProvider provider = new();
        provider._fun = (x, y, z) => GetValue(x, y, z) + other.GetValue(x, y, z);
        return provider;
    }

    public ValueProvider Subtract(ValueProvider other)
    {
        ValueProvider provider = new();
        provider._fun = (x, y, z) => GetValue(x, y, z) - other.GetValue(x, y, z);
        return provider;
    }

    public ValueProvider Multiply(ValueProvider other)
    {
        ValueProvider provider = new();
        provider._fun = (x, y, z) => GetValue(x, y, z) * other.GetValue(x, y, z);
        return provider;
    }

    public ValueProvider Divide(ValueProvider other)
    {
        ValueProvider provider = new();
        provider._fun = (x, y, z) => GetValue(x, y, z) / other.GetValue(x, y, z);
        return provider;
    }

    public static ValueProvider operator +(ValueProvider a, ValueProvider b) => a.Add(b);
    public static ValueProvider operator -(ValueProvider a, ValueProvider b) => a.Subtract(b);
    public static ValueProvider operator *(ValueProvider a, ValueProvider b) => a.Multiply(b);
    public static ValueProvider operator /(ValueProvider a, ValueProvider b) => a.Divide(b);

#endregion

}
