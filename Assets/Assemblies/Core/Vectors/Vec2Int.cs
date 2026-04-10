#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Larnix.Core.Vectors;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Vec2Int
{
    public int x { get; }
    public int y { get; }

    public Vec2Int(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public Vec2 ToVec2()
    {
        return new Vec2(x, y);
    }

    public double Magnitude => ToVec2().Magnitude;
    public double SqrMagnitude => ToVec2().SqrMagnitude;

    public static Vec2Int Zero => new(0, 0);
    public static Vec2Int One => new(1, 1);

    public static Vec2Int Up => new(0, 1);
    public static Vec2Int Down => new(0, -1);
    public static Vec2Int Left => new(-1, 0);
    public static Vec2Int Right => new(1, 0);

    public static Vec2Int[] CardinalDirections => new[] { Up, Right, Down, Left };

    public static int ManhattanDistance(Vec2Int v1, Vec2Int v2)
    {
        long a = Math.Abs(v1.x - v2.x);
        long b = Math.Abs(v1.y - v2.y);
        long result = a + b;

        return (int)Math.Min(result, int.MaxValue);
    }

    public static Vec2Int MinCorner(Vec2Int a, Vec2Int b) => new(Math.Min(a.x, b.x), Math.Min(a.y, b.y));
    public static Vec2Int MaxCorner(Vec2Int a, Vec2Int b) => new(Math.Max(a.x, b.x), Math.Max(a.y, b.y));

    public override string ToString() => $"({x}, {y})";
    public static explicit operator string(Vec2Int value) => value.ToString();
    public static implicit operator Vec2(Vec2Int value) => value.ToVec2();

    public static Vec2Int operator +(Vec2Int a, Vec2Int b) => new(a.x + b.x, a.y + b.y);
    public static Vec2Int operator -(Vec2Int a, Vec2Int b) => new(a.x - b.x, a.y - b.y);
    public static Vec2Int operator *(Vec2Int a, int scalar) => new(a.x * scalar, a.y * scalar);
    public static Vec2Int operator *(int scalar, Vec2Int a) => new(a.x * scalar, a.y * scalar);
    public static Vec2Int operator /(Vec2Int a, int scalar) => new(a.x / scalar, a.y / scalar);
    public static Vec2Int operator -(Vec2Int a) => new(-a.x, -a.y);
}
