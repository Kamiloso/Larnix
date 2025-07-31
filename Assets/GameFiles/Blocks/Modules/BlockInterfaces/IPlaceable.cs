using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Modules.Blocks
{
    public interface IPlaceable
    {
        void Init()
        {

        }

        bool ALLOW_PLACE_BACK();

        public bool STATIC_IsPlaceable(SingleBlockData block, bool front)
        {
            return front || ALLOW_PLACE_BACK();
        }
    }
}
