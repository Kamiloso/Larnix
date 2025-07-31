using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Modules.Blocks
{
    public interface IReplaceable
    {
        void Init()
        {

        }

        public bool STATIC_IsReplaceable(SingleBlockData block, bool front)
        {
            return true;
        }
    }
}
