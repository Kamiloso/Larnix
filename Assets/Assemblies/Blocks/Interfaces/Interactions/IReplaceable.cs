using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public interface IReplaceable : IBlockInterface
    {
        public bool STATIC_IsReplaceable(BlockData1 thisBlock, BlockData1 otherBlock, bool isFront)
        {
            return true;
        }
    }
}
