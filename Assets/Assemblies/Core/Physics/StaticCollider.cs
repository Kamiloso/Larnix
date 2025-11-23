using Larnix;
using UnityEngine;
using Larnix.Core.Vectors;

namespace Larnix.Core.Physics
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

        public static StaticCollider Create(Vec2 size, Vec2 offset, Vector2Int POS)
        {
            return new StaticCollider(
                new Vec2(POS.x + offset.x, POS.y + offset.y),
                new Vec2(size.x, size.y)
                );
        }
    }
}
