using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Core.Binary;

namespace Larnix.Blocks
{
    public class BlockInteractions
    {
        public static bool CanBePlaced(BlockData2 blockPair, BlockData1 item, bool front)
        {
            BlockData1 frontBlock = blockPair.Front;
            BlockData1 backBlock = blockPair.Back;
            BlockData1 block = front ? frontBlock : backBlock;

            bool can_place = BlockFactory.GetSlaveInstance<IPlaceable>(item.ID)?.STATIC_IsPlaceable(item, front) == true;
            bool can_replace = BlockFactory.GetSlaveInstance<IReplaceable>(block.ID)?.STATIC_IsReplaceable(block, item, front) == true;
            bool blocking_front = BlockFactory.GetSlaveInstance<IBlockingFront>(frontBlock.ID)?.IS_BLOCKING_FRONT() == true;

            return can_place && can_replace && (front || !blocking_front);
        }

        public static bool CanBeBroken(BlockData2 blockPair, BlockData1 item, BlockData1 tool, bool front)
        {
            BlockData1 frontBlock = blockPair.Front;
            BlockData1 backBlock = blockPair.Back;
            BlockData1 block = front ? frontBlock : backBlock;

            bool is_breakable = BlockFactory.GetSlaveInstance<IBreakable>(block.ID)?.STATIC_IsBreakable(block, tool, front) == true;
            bool is_breakable_match = BlockFactory.GetSlaveInstance<IBreakable>(block.ID)?.STATIC_IsBreakableItemMatch(block, item) == true;
            bool blocking_front = BlockFactory.GetSlaveInstance<IBlockingFront>(frontBlock.ID)?.IS_BLOCKING_FRONT() == true;
                
            return is_breakable && is_breakable_match && (front || !blocking_front);
        }
    }
}
