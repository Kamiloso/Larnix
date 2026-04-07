#nullable enable
using System;
using Larnix.Core.Binary;
using System.Globalization;
using Larnix.Core.Utils;
using System.Runtime.InteropServices;

namespace Larnix.Core.Vectors;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Vec2 : IBinary<Vec2>, IFixedStruct<Vec2>
{
    public const int SIZE = sizeof(double) * 2;

    public double x { get; }
    public double y { get; }

    public Vec2(double x, double y)
    {
        this.x = double.IsFinite(x) ? x : 0.0;
        this.y = double.IsFinite(y) ? y : 0.0;
    }

    public Vec2 Sanitize()
    {
        return new Vec2(
            double.IsFinite(x) ? x : 0.0,
            double.IsFinite(y) ? y : 0.0
        );
    }

    public byte[] Serialize()
    {
        return ArrayUtils.MegaConcat(
            Primitives.GetBytes(x),
            Primitives.GetBytes(y)
        );
    }

    public bool Deserialize(byte[] bytes, int offset, out Vec2 result)
    {
        if (offset < 0 || offset + SIZE > bytes.Length)
        {
            result = default;
            return false;
        }

        double _x = Primitives.FromBytes<double>(bytes, offset);
        double _y = Primitives.FromBytes<double>(bytes, offset + 8);

        result = new Vec2(
            double.IsFinite(_x) ? _x : 0.0,
            double.IsFinite(_y) ? _y : 0.0
        );
        return true;
    }

    public static Vec2 Zero => new Vec2(0, 0);
    public static Vec2 One => new Vec2(1, 1);
    public double Magnitude => Math.Sqrt(x * x + y * y);
    public double SqrMagnitude => x * x + y * y;
    public Vec2 Normalized
    {
        get
        {
            var mag = Magnitude;
            return mag > 0 ? this / mag : new Vec2(0, 0);
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
