using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using System;

namespace Larnix.Blocks
{
    public interface IRotational : IBlockInterface
    {
        const byte ROTATION_MASK = 0b0011;

        byte STATIC_RotationVariant(byte variant) => (byte)(variant & ~ROTATION_MASK);
        Vec2Int STATIC_RotationDirection(byte variant) => (variant & ROTATION_MASK) switch
        {
            0b00 => Vec2Int.Up,
            0b01 => Vec2Int.Right,
            0b10 => Vec2Int.Down,
            0b11 => Vec2Int.Left,
            _ => throw new IndexOutOfRangeException("Invalid rotation variant!")
        };
    }
}
