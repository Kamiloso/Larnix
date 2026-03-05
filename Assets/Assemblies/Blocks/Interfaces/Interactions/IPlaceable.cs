using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public interface IPlaceable : IBlockInterface
    {
        bool ALLOW_PLACE_BACK();
        bool HAS_PLACE_PARTICLES() => false;

        public bool STATIC_IsPlaceable(BlockData1 block, bool front)
        {
            return front || ALLOW_PLACE_BACK();
        }
    }
}
