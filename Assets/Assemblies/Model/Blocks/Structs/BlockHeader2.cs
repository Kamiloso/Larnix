#nullable enable
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;

namespace Larnix.Model.Blocks.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct BlockHeader2 : ISanitizable<BlockHeader2>
{
    private readonly BlockID _idFront;
    private readonly BlockID _idBack;
    private readonly byte _infoByte;

    public BlockHeader1 Front => new(_idFront, (byte)(_infoByte >> 4));
    public BlockHeader1 Back => new(_idBack, (byte)(_infoByte & 0x0F));

    public static BlockHeader2 Empty => new(
        BlockHeader1.Air,
        BlockHeader1.Air
        );

    public BlockHeader2(BlockHeader1 front, BlockHeader1 back)
    {
        front = Sanitizer.Filter(front);
        back = Sanitizer.Filter(back);

        _idFront = front.Id;
        _idBack = back.Id;
        _infoByte = (byte)((front.Variant << 4) | back.Variant);
    }

    public BlockHeader2 Sanitize()
    {
        return new BlockHeader2(Front, Back);
    }

    public override string ToString()
    {
        return $"({Front}, {Back})";
    }
}
