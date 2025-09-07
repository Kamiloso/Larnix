using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Entities
{
    public class Player : EntityServer, IHasCollider
    {
        public Player(ulong uid, EntityData entityData)
            : base(uid, entityData) { }

        Vec2 IHasCollider.COLLIDER_OFFSET() => new Vec2(0.00, 0.375);
        Vec2 IHasCollider.COLLIDER_SIZE() => new Vec2(0.40, 1.75);

        public void UpdateTransform(Vec2 position, float rotation)
        {
            SetTransform(position, rotation);
        }

        public void AcceptTeleport(Vec2 targetPosition)
        {

        }
    }
}
