using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Blocks
{
    public interface IPlaceable : IBlockInterface
    {
        void Init()
        {

        }

        bool ALLOW_PLACE_BACK();

        public bool STATIC_IsPlaceable(BlockData1 block, bool front)
        {
            return front || ALLOW_PLACE_BACK();
        }
    }
}
