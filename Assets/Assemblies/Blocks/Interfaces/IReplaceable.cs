using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Blocks
{
    public interface IReplaceable : IBlockInterface
    {
        void Init()
        {

        }

        public bool STATIC_IsReplaceable(BlockData1 block, bool front)
        {
            return true;
        }
    }
}
