#nullable enable
using System;
using Larnix.Core.Binary;
using Larnix.Core.Utils;

namespace Larnix.Model.Blocks.Structs;

public readonly struct BlockHeader1 : IBinary<BlockHeader1>, IEquatable<BlockHeader1>
{
    public const int SIZE = sizeof(BlockID) + sizeof(byte);

    public const byte MAX_VARIANT = 0b0000_1111; // 4 bits for variant (0-15)
    private static byte Reduce(byte value) => (byte)(value & MAX_VARIANT);

    public BlockID ID { get; }
    public byte Variant { get; }

    public static BlockHeader1 Air => new(BlockID.Air);
    public static BlockHeader1 UltimateTool => new(BlockID.UltimateTool);

    public BlockHeader1(BlockID id, byte variant = 0)
    {
        ID = id;
        Variant = Reduce(variant);
    }

    public bool Deserialize(byte[] bytes, int offset, out BlockHeader1 result)
    {
        if (offset < 0 || offset + SIZE > bytes.Length)
        {
            result = default;
            return false;
        }

        BlockID id = Primitives.FromBytes<BlockID>(bytes, offset);
        offset += sizeof(BlockID);

        byte variant = Reduce(Primitives.FromBytes<byte>(bytes, offset));
        offset += sizeof(byte);

        result = new BlockHeader1(id, variant);
        return true;
    }

    public byte[] Serialize()
    {
        return ArrayUtils.MegaConcat(
            Primitives.GetBytes(ID),
            Primitives.GetBytes(Variant)
        );
    }

    public bool Equals(BlockHeader1 other)
    {
        return ID == other.ID && Variant == other.Variant;
    }

    public override bool Equals(object obj)
    {
        return obj is BlockHeader1 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ID, Variant);
    }

    public static bool operator ==(BlockHeader1 left, BlockHeader1 right) => left.Equals(right);
    public static bool operator !=(BlockHeader1 left, BlockHeader1 right) => !left.Equals(right);

    public override string ToString()
    {
        return Variant > 0 ?
            $"{ID}:{Variant}" :
            $"{ID}";
    }
}
