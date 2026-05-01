#nullable enable

namespace Larnix.Model.Blocks.Structs;

public class Item
{
    public BlockData1 Block { get; set; }
    public int Count { get; set; }

    public Item(BlockData1 block, int count)
    {
        Block = block;
        Count = count;
    }

    public Item DeepCopy()
    {
        return new Item(Block.DeepCopy(), Count);
    }
}
