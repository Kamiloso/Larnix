#nullable enable
using System;
using Larnix.Core.Binary;
using Larnix.Core.Utils;

namespace Larnix.Model.Blocks.Structs;

public readonly struct BlockHeader2 : IBinary<BlockHeader2>, IEquatable<BlockHeader2>
{
    public const int SIZE = sizeof(BlockID) * 2 + sizeof(byte);

    public BlockHeader1 Front { get; }
    public BlockHeader1 Back { get; }

    public static BlockHeader2 Empty => new();

    public BlockHeader2(BlockHeader1 front, BlockHeader1 back)
    {
        Front = front;
        Back = back;
    }

    public bool Deserialize(byte[] data, int offset, out BlockHeader2 result)
    {
        if (offset < 0 || offset + SIZE > data.Length)
        {
            result = default;
            return false;
        }

        byte variants = data[4 + offset];

        BlockHeader1 front = new BlockHeader1(
            Primitives.FromBytes<BlockID>(data, 0 + offset),
            (byte)(variants / 16)
        );

        BlockHeader1 back = new BlockHeader1(
            Primitives.FromBytes<BlockID>(data, 2 + offset),
            (byte)(variants % 16)
        );

        result = new BlockHeader2(front, back);
        return true;
    }

    public byte[] Serialize()
    {
        return ArrayUtils.MegaConcat(
            Primitives.GetBytes(Front.ID),
            Primitives.GetBytes(Back.ID),
            new byte[] { (byte)(16 * Front.Variant + Back.Variant) }
        );
    }

    public bool Equals(BlockHeader2 other)
    {
        return Front == other.Front && Back == other.Back;
    }

    public override bool Equals(object obj)
    {
        return obj is BlockHeader2 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Front, Back);
    }

    public static bool operator ==(BlockHeader2 left, BlockHeader2 right) => left.Equals(right);
    public static bool operator !=(BlockHeader2 left, BlockHeader2 right) => !left.Equals(right);

    public override string ToString()
    {
        return $"({Front}, {Back})";
    }
}
