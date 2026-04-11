#nullable enable
using System.Runtime.InteropServices;

namespace Larnix.Model.Blocks.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct BlockHeader2
{
    private BlockID IdFront { get; }
    private BlockID IdBack { get; }
    private byte InfoByte { get; }

    public BlockHeader1 Front => new(IdFront, (byte)(InfoByte >> 4));
    public BlockHeader1 Back => new(IdBack, (byte)(InfoByte & 0x0F));

    public static BlockHeader2 Empty => new();

    public BlockHeader2(BlockHeader1 front, BlockHeader1 back)
    {
        IdFront = front.Id;
        IdBack = back.Id;
        InfoByte = (byte)((front.Variant << 4) | back.Variant);
    }

    public override string ToString()
    {
        return $"({Front}, {Back})";
    }
}
