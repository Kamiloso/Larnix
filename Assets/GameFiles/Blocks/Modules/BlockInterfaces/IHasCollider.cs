using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Physics;

namespace Larnix.Modules.Blocks
{
    public interface IHasCollider
    {
        float COLLIDER_WIDTH();
        float COLLIDER_HEIGHT();

        StaticCollider STATIC_CreateStaticCollider(byte variant = 0)
        {
            return new StaticCollider(Vector2.zero, new Vector2(COLLIDER_WIDTH(), COLLIDER_HEIGHT()));
        }
    }
}
