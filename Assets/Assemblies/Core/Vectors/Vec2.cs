#nullable enable
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Larnix.Core.Vectors;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Vec2 : IFixedStruct<Vec2>
{
    private readonly double _x;
    private readonly double _y;

    public double x => double.IsFinite(_x) ? _x : 0.0;
    public double y => double.IsFinite(_y) ? _y : 0.0;

    public Vec2(double x, double y)
    {
        _x = x;
        _y = y;
    }

    public static Vec2 Zero => new(0, 0);
    public static Vec2 One => new(1, 1);
    public double Magnitude => Math.Sqrt(x * x + y * y);
    public double SqrMagnitude => x * x + y * y;
    public Vec2 Normalized
    {
        get
        {
            var mag = Magnitude;
            return mag > 0 ? this / mag : new Vec2(1, 0);
        }
    }

    public static double Distance(Vec2 a, Vec2 b) => (a - b).Magnitude;

    public override string ToString() => $"({x.ToString(CultureInfo.InvariantCulture)}, {y.ToString(CultureInfo.InvariantCulture)})";
    public static implicit operator string(Vec2 value) => value.ToString();

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.x + b.x, a.y + b.y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.x - b.x, a.y - b.y);
    public static Vec2 operator *(Vec2 a, double s) => new(a.x * s, a.y * s);
    public static Vec2 operator *(double s, Vec2 a) => new(a.x * s, a.y * s);
    public static Vec2 operator /(Vec2 a, double s) => new(a.x / s, a.y / s);
    public static Vec2 operator -(Vec2 a) => new(-a.x, -a.y);
}
