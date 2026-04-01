using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Model.Physics;

namespace Larnix.Model.Entities.All;

public interface IHasCollider : IEntityInterface
{
    Vec2 COLLIDER_SIZE();
    Vec2 COLLIDER_OFFSET();
}
