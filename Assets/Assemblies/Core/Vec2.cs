using UnityEngine;
using QuickNet;
using System;

public struct Vec2 : IEquatable<Vec2>
{
    public const int ORIGIN_STEP = 16 * 64;
    public readonly double x;
    public readonly double y;

    public Vec2(double x, double y)
    {
        this.x = x;
        this.y = y;
    }

    public Vec2(Vector2 pos, Vec2 origin)
    {
        x = origin.x + pos.x;
        y = origin.y + pos.y;
    }

    public Vec2 ExtractOrigin()
    {
        Vector2Int middleblock = ORIGIN_STEP * ExtractSector() + ORIGIN_STEP / 2 * Vector2Int.one;
        return new Vec2(
            middleblock.x - 0.5,
            middleblock.y - 0.5
            );
    }

    public Vector2 ExtractPosition(Vec2 origin)
    {
        return new Vector2(
            (float)(x - origin.x),
            (float)(y - origin.y)
        );
    }

    public Vector2Int ExtractSector()
    {
        try
        {
            return new Vector2Int(
                (int)Math.Floor((x + 0.5) / ORIGIN_STEP),
                (int)Math.Floor((y + 0.5) / ORIGIN_STEP)
            );
        }
        catch (OverflowException)
        {
            return default;
        }
    }

    public byte[] Serialize()
    {
        return ArrayUtils.MegaConcat(
            EndianUnsafe.GetBytes(x),
            EndianUnsafe.GetBytes(y)
        );
    }

    public static Vec2 Deserialize(byte[] bytes, int offset = 0)
    {
        return new Vec2(
            EndianUnsafe.FromBytes<double>(bytes, offset),
            EndianUnsafe.FromBytes<double>(bytes, offset + 8)
        );
    }

    public static Vec2 Zero => new Vec2( 0, 0 );
    public static Vec2 One => new Vec2( 1, 1 );
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

    public static Vec2 Lerp(Vec2 a, Vec2 b, float t)
    {
        t = Mathf.Clamp01(t);
        return new Vec2(
            a.x + (b.x - a.x) * t,
            a.y + (b.y - a.y) * t
        );
    }

    public override string ToString() => $"({x}, {y})";
    public static implicit operator string(Vec2 value) => value.ToString();

    public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.x + b.x, a.y + b.y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.x - b.x, a.y - b.y);
    public static Vec2 operator *(Vec2 a, double s) => new Vec2(a.x * s, a.y * s);
    public static Vec2 operator *(double s, Vec2 a) => new Vec2(a.x * s, a.y * s);
    public static Vec2 operator /(Vec2 a, double s) => new Vec2(a.x / s, a.y / s);
    public static Vec2 operator -(Vec2 a) => new Vec2(-a.x, -a.y);
    public static bool operator ==(Vec2 a, Vec2 b) => a.Equals(b);
    public static bool operator !=(Vec2 a, Vec2 b) => !a.Equals(b);

    public override bool Equals(object obj) => obj is Vec2 v && Equals(v);

    public bool Equals(Vec2 other) =>
        x.Equals(other.x) && y.Equals(other.y);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + x.GetHashCode();
            hash = hash * 31 + y.GetHashCode();
            return hash;
        }
    }
}
