using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using Larnix.GameCore.Structs;

namespace Larnix.Blocks.All;

public interface IReplaceable : IBlockInterface
{
    public bool STATIC_IsReplaceable(BlockHeader1 thisBlock, BlockHeader1 otherBlock, bool isFront)
    {
        return true;
    }
}
