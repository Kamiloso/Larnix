#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Physics;
using Larnix.Model.Physics.Structs;

namespace Larnix.Model.Entities.All;

public sealed class Player : Entity, IHasCollider, IPhysicsProperties
{
    Vec2 IHasCollider.COLLIDER_OFFSET() => new(0.00, 0.375);
    Vec2 IHasCollider.COLLIDER_SIZE() => new(0.40, 1.75);

    double IPhysicsProperties.JUMP_SIZE() => 25.0;
    double IPhysicsProperties.MAX_HORIZONTAL_VELOCITY() => 15.0;

    public static DynamicCollider MakeDynamicCollider(PhysicsManager physics, Vec2 position)
    {
        Player slave = EntityFactory.GetSlaveInstance<Player>(EntityID.Player)!;

        Vec2 offset = ((IHasCollider)slave).COLLIDER_OFFSET();
        Vec2 size = ((IHasCollider)slave).COLLIDER_SIZE();

        PhysicsProperties phProp = ((IPhysicsProperties)slave).PHYSICS_PROPERTIES();

        return new DynamicCollider(physics, position, offset, size, phProp);
    }
}
