#nullable enable
using Larnix.Model.Blocks.All;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Model.Blocks;

public static class BlockInteractions
{
    public static bool CanBePlaced(BlockHeader2 blockPair, BlockHeader1 item, bool front)
    {
        BlockHeader1 frontBlock = blockPair.Front;
        BlockHeader1 backBlock = blockPair.Back;
        BlockHeader1 block = front ? frontBlock : backBlock;

        bool can_place = BlockFactory.GetSlaveInstance<IPlaceable>(item.Id)?.STATIC_IsPlaceable(item, front) == true;
        bool can_replace = BlockFactory.GetSlaveInstance<IReplaceable>(block.Id)?.STATIC_IsReplaceable(block, item, front) == true;
        bool blocking_front = BlockFactory.GetSlaveInstance<IBlockingFront>(frontBlock.Id)?.IS_BLOCKING_FRONT() == true;

        return can_place && can_replace && (front || !blocking_front);
    }

    public static bool CanBeBroken(BlockHeader2 blockPair, BlockHeader1 item, BlockHeader1 tool, bool front)
    {
        BlockHeader1 frontBlock = blockPair.Front;
        BlockHeader1 backBlock = blockPair.Back;
        BlockHeader1 block = front ? frontBlock : backBlock;

        bool is_breakable = BlockFactory.GetSlaveInstance<IBreakable>(block.Id)?.STATIC_IsBreakable(block, tool, front) == true;
        bool is_breakable_match = BlockFactory.GetSlaveInstance<IBreakable>(block.Id)?.STATIC_IsBreakableItemMatch(block, item) == true;
        bool blocking_front = BlockFactory.GetSlaveInstance<IBlockingFront>(frontBlock.Id)?.IS_BLOCKING_FRONT() == true;

        return is_breakable && is_breakable_match && (front || !blocking_front);
    }
}
