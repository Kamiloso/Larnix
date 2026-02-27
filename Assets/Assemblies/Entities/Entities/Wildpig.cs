using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Physics;
using Larnix.Core.Vectors;

namespace Larnix.Entities.All
{
    public sealed class Wildpig : Entity, IWalking
    {
        DynamicCollider IPhysics.dynamicCollider { get; set; }
        OutputData? IPhysics.outputData { get; set; }

        Vec2 IHasCollider.COLLIDER_OFFSET() => new Vec2(0.00, 0.2);
        Vec2 IHasCollider.COLLIDER_SIZE() => new Vec2(0.75, 1.40);

        int IWalking.MIN_THINKING_TIME() => 50;
        int IWalking.MAX_THINKING_TIME() => 100;
    }
}
