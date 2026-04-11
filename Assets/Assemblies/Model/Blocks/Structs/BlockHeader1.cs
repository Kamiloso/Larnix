#nullable enable
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;

namespace Larnix.Model.Blocks.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct BlockHeader1 : ISanitizable<BlockHeader1>
{
    public BlockID Id { get; }
    public byte Variant { get; }

    public static BlockHeader1 Air => new(BlockID.Air);
    public static BlockHeader1 UltimateTool => new(BlockID.UltimateTool);

    public BlockHeader1(BlockID id, byte variant = 0)
    {
        Id = id;
        Variant = (byte)(variant & 0x0F);
    }

    public BlockHeader1 Sanitize()
    {
        return new BlockHeader1(Id, Variant);
    }

    public override string ToString()
    {
        return Variant > 0 ?
            $"{Id}:{Variant}" :
            $"{Id}";
    }
}
