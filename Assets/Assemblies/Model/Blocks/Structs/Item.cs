#nullable enable
using System;

namespace Larnix.Model.Blocks.Structs;

public class Item
{
    public BlockData1 Block { get; set; }
    public int Count { get; set; }

    public Item(BlockData1 block, int count)
    {
        Block = block ?? throw new ArgumentNullException(nameof(block));
        Count = count;
    }

    public Item DeepCopy()
    {
        return new Item(Block.DeepCopy(), Count);
    }
}
