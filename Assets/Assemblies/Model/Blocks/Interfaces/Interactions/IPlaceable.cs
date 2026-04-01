using Larnix.Model.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Model.Blocks.All;

public interface IPlaceable : IBlockInterface
{
    bool ALLOW_PLACE_BACK();
    bool HAS_PLACE_PARTICLES() => false;

    public bool STATIC_IsPlaceable(BlockHeader1 block, bool front)
    {
        return front || ALLOW_PLACE_BACK();
    }
}
