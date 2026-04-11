#nullable enable
using Larnix.Core.Vectors;
using System.Runtime.InteropServices;
using System;
using Larnix.Model.Entities;
using Larnix.Model.Entities.Structs;

namespace Larnix.Server.Packets.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct EntityHeaderCompressed
{
    private readonly EntityID _id;
    private readonly int _xi;
    private readonly byte _xf;
    private readonly int _yi;
    private readonly byte _yf;
    private readonly byte _rt;

    public EntityHeader Header => new(Id, Position, Rotation);

    public EntityID Id => _id;
    public Vec2 Position => new(
        x: _xi + (_xf / 256.0),
        y: _yi + (_yf / 256.0)
    );
    public float Rotation => _rt * (360f / 256f);

    public EntityHeaderCompressed(in EntityHeader header)
    {
        _id = header.Id;

        double px = Math.Clamp(header.Position.x, int.MinValue + 1, int.MaxValue - 1);
        double py = Math.Clamp(header.Position.y, int.MinValue + 1, int.MaxValue - 1);

        _xi = (int)Math.Floor(px);
        _xf = (byte)Math.Min((px - _xi) * 256.0, 255.0);

        _yi = (int)Math.Floor(py);
        _yf = (byte)Math.Min((py - _yi) * 256.0, 255.0);

        _rt = EncodeRotation(header.Rotation);
    }

    private static byte EncodeRotation(float rotation)
    {
        float normalized = rotation % 360f;
        if (normalized < 0f)
        {
            normalized += 360f; // [0, 360)
        }

        return (byte)Math.Min(normalized * (256f / 360f), 255f);
    }

    public override string ToString()
    {
        return Id.ToString();
    }
}
