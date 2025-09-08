using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Entities
{
    public interface IHasCollider : IEntityInterface
    {
        Vec2 COLLIDER_SIZE();
        Vec2 COLLIDER_OFFSET();
    }
}
