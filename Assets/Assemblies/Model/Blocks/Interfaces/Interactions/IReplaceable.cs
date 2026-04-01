using Larnix.Model.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Model.Blocks.All;

public interface IReplaceable : IBlockInterface
{
    public bool STATIC_IsReplaceable(BlockHeader1 thisBlock, BlockHeader1 otherBlock, bool isFront)
    {
        return true;
    }
}
