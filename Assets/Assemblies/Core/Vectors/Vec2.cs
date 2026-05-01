#nullable enable
using Larnix.Core.Serialization;
using Larnix.Core.Utils;
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Larnix.Core.Vectors;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Vec2 : ISanitizable<Vec2>
{
    public double x { get; }
    public double y { get; }

    public double Magnitude => Math.Sqrt(x * x + y * y);
    public double SqrMagnitude => x * x + y * y;

    public static Vec2 Zero => new(0, 0);
    public static Vec2 One => new(1, 1);

    public Vec2(double x, double y)
    {
        this.x = Sanitizer.ToFinite(x);
        this.y = Sanitizer.ToFinite(y);
    }

    public Vec2 Sanitize()
    {
        return new Vec2(x, y);
    }

    public Vec2 Normalize()
    {
        double mag = Magnitude;
        return mag > 0 ? this / mag : new Vec2(1, 0);
    }

    public static double Distance(Vec2 a, Vec2 b)
    {
        return (a - b).Magnitude;
    }

    public override string ToString()
    {
        string s1 = x.ToString(CultureInfo.InvariantCulture);
        string s2 = y.ToString(CultureInfo.InvariantCulture);

        return $"({s1}, {s2})";
    }

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.x + b.x, a.y + b.y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.x - b.x, a.y - b.y);
    public static Vec2 operator *(Vec2 a, double s) => new(a.x * s, a.y * s);
    public static Vec2 operator *(double s, Vec2 a) => new(a.x * s, a.y * s);
    public static Vec2 operator /(Vec2 a, double s) => new(a.x / s, a.y / s);
    public static Vec2 operator -(Vec2 a) => new(-a.x, -a.y);
}
