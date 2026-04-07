#nullable enable
using System;
using System.Runtime.InteropServices;
using Larnix.Core.Binary;

namespace Larnix.Core.Vectors;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Col32(byte r, byte g, byte b, byte a) : IFixedStruct<Col32>
{
    public const int SIZE = sizeof(byte) * 4;

    public static Col32 Red => new(255, 0, 0, 255);
    public static Col32 Green => new(0, 255, 0, 255);
    public static Col32 Blue => new(0, 0, 255, 255);
    public static Col32 White => new(255, 255, 255, 255);
    public static Col32 Black => new(0, 0, 0, 255);
    public static Col32 Yellow => new(255, 255, 0, 255);
    public static Col32 Cyan => new(0, 255, 255, 255);
    public static Col32 Magenta => new(255, 0, 255, 255);
    public static Col32 Transparent => new(0, 0, 0, 0);

    public static Col32 Lerp(Col32 start, Col32 end, double t = 0.5f)
    {
        t = Math.Clamp(t, 0f, 1f);
        byte lerp(byte a, byte b) => (byte)(a + (b - a) * t);

        return new Col32(
            lerp(start.r, end.r),
            lerp(start.g, end.g),
            lerp(start.b, end.b),
            lerp(start.a, end.a)
        );
    }

    public override string ToString() => $"(R: {r}, G: {g}, B: {b}, A: {a})";
}
