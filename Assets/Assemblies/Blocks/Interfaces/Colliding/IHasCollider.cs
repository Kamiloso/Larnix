using System.Collections;
using System.Collections.Generic;

namespace Larnix.Blocks
{
    public interface IHasCollider : IBlockInterface
    {
        float COLLIDER_OFFSET_X();
        float COLLIDER_OFFSET_Y();
        float COLLIDER_WIDTH();
        float COLLIDER_HEIGHT();
    }
}
