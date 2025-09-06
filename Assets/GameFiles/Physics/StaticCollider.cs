using Larnix;
using UnityEngine;
using Larnix.Blocks;
using Larnix.Core;

namespace Larnix.Physics
{
    public class StaticCollider
    {
        public readonly Vec2 Center;
        public readonly Vec2 Size;

        public StaticCollider(Vec2 center, Vec2 size)
        {
            Center = center;
            Size = size;
        }

        public static StaticCollider Create(IHasCollider iface, Vector2Int POS)
        {
            return new StaticCollider(
                new Vec2(POS.x + (double)iface.COLLIDER_OFFSET_X(), POS.y + (double)iface.COLLIDER_OFFSET_Y()),
                new Vec2(iface.COLLIDER_WIDTH(), iface.COLLIDER_HEIGHT())
                );
        }
    }
}
