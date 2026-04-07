#nullable enable
using System.Runtime.InteropServices;
using Larnix.Core;

namespace Larnix.Model.Blocks.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct BlockHeader1 : IFixedStruct<BlockHeader1>
{
    public const int MAX_VARIANT = 0x0F; // 15

    public readonly BlockID ID;
    public readonly byte Variant;

    public static BlockHeader1 Air => new(BlockID.Air);
    public static BlockHeader1 UltimateTool => new(BlockID.UltimateTool);

    public BlockHeader1(BlockID id, byte variant = 0)
    {
        ID = id;
        Variant = (byte)(variant & 0x0F);
    }

    public BlockHeader1 Sanitize() => new(ID, Variant);

    public override string ToString()
    {
        return Variant > 0 ?
            $"{ID}:{Variant}" :
            $"{ID}";
    }
}
