using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Entities.Structs;

namespace Larnix.Entities.All
{
    public sealed class Player : EntityServer, IHasCollider, IPhysicsProperties
    {
        public Player(ulong uid, EntityData entityData)
            : base(uid, entityData) { }

        Vec2 IHasCollider.COLLIDER_OFFSET() => new Vec2(0.00, 0.375);
        Vec2 IHasCollider.COLLIDER_SIZE() => new Vec2(0.40, 1.75);

        double IPhysicsProperties.JUMP_SIZE() => 25.0;
        double IPhysicsProperties.MAX_HORIZONTAL_VELOCITY() => 15.0;

        public void UpdateTransform(Vec2 position, float rotation)
        {
            SetTransform(position, rotation);
        }

        public void AcceptTeleport(Vec2 targetPosition)
        {
            // silent acceptance... (for now)
        }
    }
}
