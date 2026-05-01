#nullable enable
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;

namespace Larnix.Model.Blocks.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct BlockHeader2 : ISanitizable<BlockHeader2>
{
    private BlockID IdFront { get; }
    private BlockID IdBack { get; }
    private byte InfoByte { get; }

    public BlockHeader1 Front => new(IdFront, (byte)(InfoByte >> 4));
    public BlockHeader1 Back => new(IdBack, (byte)(InfoByte & 0x0F));

    public static BlockHeader2 Empty => default;

    private BlockHeader2(BlockID idFront, BlockID idBack, byte infoByte)
    {
        IdFront = Sanitizer.Filter(idFront);
        IdBack = Sanitizer.Filter(idBack);
        InfoByte = Sanitizer.Filter(infoByte);
    }

    public BlockHeader2(BlockHeader1 front, BlockHeader1 back)
    {
        byte b1 = Sanitizer.ToHalfByte(front.Variant);
        byte b2 = Sanitizer.ToHalfByte(back.Variant);
        byte infoByte = (byte)((b1 << 4) | b2);

        this = new BlockHeader2(front.Id, back.Id, infoByte);
    }

    public BlockHeader2 Sanitize()
    {
        return new BlockHeader2(IdFront, IdBack, InfoByte);
    }

    public override string ToString()
    {
        return $"({Front}, {Back})";
    }
}
