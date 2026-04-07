#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Utils;

namespace Larnix.Model.Physics;

public class StaticCollider
{
    public Vec2 Center { get; }
    public Vec2 Size { get; }

    public StaticCollider(Vec2 center, Vec2 size)
    {
        ColliderUtils.AssertSizePositive(size);

        Center = center;
        Size = size + Common.WorldEpsilon;
    }
}
