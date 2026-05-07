#nullable enable

namespace Larnix.Model.Blocks.Structs;

public class BlockData2
{
    public BlockData1 Front { get; set; }
    public BlockData1 Back { get; set; }

    public BlockHeader2 Header => new(Front.Header, Back.Header);

    public static BlockData2 Empty => new(BlockData1.Air, BlockData1.Air);

    public BlockData2(BlockData1 front, BlockData1 back)
    {
        Front = front;
        Back = back;
    }

    public BlockData2(in BlockHeader2 header)
    {
        Front = new BlockData1(header.Front);
        Back = new BlockData1(header.Back);
    }

    public BlockData2 DeepCopy()
    {
        return new BlockData2(Front.DeepCopy(), Back.DeepCopy());
    }
}
