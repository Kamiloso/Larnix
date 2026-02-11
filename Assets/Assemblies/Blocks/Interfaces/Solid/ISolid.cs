using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;

namespace Larnix.Blocks
{
    public interface ISolid : IHasCollider, IPlaceable, IBreakable, IBlockingFront, IHasConture
    {
        IEnumerable<Collider> IHasCollider.STATIC_GetAllColliders(BlockID ID, byte variant)
        {
            return new Collider[] {
                new(
                    Offset: new Vec2(0, 0),
                    Size: new Vec2(1, 1)
                )
            };
        }

        bool IBlockingFront.IS_BLOCKING_FRONT() => true;
    }
}
