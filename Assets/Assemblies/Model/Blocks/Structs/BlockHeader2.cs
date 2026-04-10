#nullable enable
using System.Runtime.InteropServices;
using Larnix.Core;

namespace Larnix.Model.Blocks.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct BlockHeader2
{
    private readonly BlockID _idFront;
    private readonly BlockID _idBack;
    private readonly byte _infoByte;

    public BlockHeader1 Front => new(_idFront, (byte)(_infoByte >> 4));
    public BlockHeader1 Back => new(_idBack, (byte)(_infoByte & 0x0F));

    public static BlockHeader2 Empty => new();

    public BlockHeader2(BlockHeader1 front, BlockHeader1 back)
    {
        _idFront = front.ID;
        _idBack = back.ID;
        _infoByte = (byte)((front.Variant << 4) | back.Variant);
    }

    public override string ToString()
    {
        return $"({Front}, {Back})";
    }
}
