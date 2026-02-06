using System.Collections;
using System.Collections.Generic;

namespace Larnix.Blocks
{
    public interface ISolid : IHasCollider, IHasConture, IBlockingFront
    {
        float IHasCollider.COLLIDER_OFFSET_X() => 0f;
        float IHasCollider.COLLIDER_OFFSET_Y() => 0f;
        float IHasCollider.COLLIDER_WIDTH() => 1f;
        float IHasCollider.COLLIDER_HEIGHT() => 1f;

        bool IBlockingFront.IS_BLOCKING_FRONT() => true;
    }
}
