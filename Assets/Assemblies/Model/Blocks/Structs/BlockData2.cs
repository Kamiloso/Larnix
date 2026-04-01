#nullable enable
using System;

namespace Larnix.Model.Blocks.Structs;

public class BlockData2
{
    private BlockData1 _front;
    private BlockData1 _back;

    public BlockData1 Front
    {
        get => _front;
        set => _front = value ?? throw new ArgumentNullException(nameof(value));
    }
    public BlockData1 Back
    {
        get => _back;
        set => _back = value ?? throw new ArgumentNullException(nameof(value));
    }

    public BlockHeader2 Header => new(Front.Header, Back.Header);

    public static BlockData2 Empty => new(BlockData1.Air, BlockData1.Air);

    public BlockData2(BlockData1 front, BlockData1 back)
    {
        _front = front ?? throw new ArgumentNullException(nameof(front));
        _back = back ?? throw new ArgumentNullException(nameof(back));
    }

    public BlockData2(in BlockHeader2 header)
    {
        _front = new BlockData1(header.Front);
        _back = new BlockData1(header.Back);
    }

    public BlockData2 DeepCopy()
    {
        return new BlockData2(Front.DeepCopy(), Back.DeepCopy());
    }
}
