using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using Larnix.Core.Physics;
using Larnix.Core.Vectors;

namespace Larnix.Blocks.All
{
    public record Collider(Vec2 Offset, Vec2 Size);
    public interface IHasCollider : IBlockInterface
    {
        Collider[] STATIC_GetAllColliders(BlockID ID, byte variant);

        public static StaticCollider MakeStaticCollider(Collider collider, Vec2Int POS)
        {
            return new StaticCollider(
                POS.ToVec2() + collider.Offset,
                collider.Size
            );
        }
    }
}
