using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Modules.Blocks
{
    public interface ISolid : IHasCollider
    {
        void Init()
        {

        }

        float IHasCollider.COLLIDER_WIDTH() => 1f;
        float IHasCollider.COLLIDER_HEIGHT() => 1f;
    }
}
