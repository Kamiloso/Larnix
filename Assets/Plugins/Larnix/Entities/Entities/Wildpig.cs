using System.Collections;
using System.Collections.Generic;
using Larnix.Physics;

namespace Larnix.Entities
{
    public class Wildpig : EntityServer, IWalkingCreature
    {
        public Wildpig(ulong uid, EntityData entityData)
            : base(uid, entityData) { }

        DynamicCollider IPhysics.dynamicCollider { get; set; }
        OutputData? IPhysics.outputData { get; set; }

        Vec2 IHasCollider.COLLIDER_OFFSET() => new Vec2(0.00, 0.30);
        Vec2 IHasCollider.COLLIDER_SIZE() => new Vec2(0.75, 1.40);
        double IWalkingCreature.JUMP_SIZE() => 10.0;
    }
}
