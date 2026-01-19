using Larnix.Core.Physics;
using System.Collections;
using System.Collections.Generic;

namespace Larnix.Entities
{
    public interface IPhysicsProperties : IEntityInterface
    {
        public PhysicsProperties PHYSICS_PROPERTIES() => new PhysicsProperties
        {
            Gravity = GRAVITY(),
            ControlForce = CONTROL_FORCE(),
            HorizontalDrag = HORIZONTAL_DRAG(),
            JumpSize = JUMP_SIZE(),
            MaxHorizontalVelocity = MAX_HORIZONTAL_VELOCITY(),
            MaxVerticalVelocity = MAX_VERTICAL_VELOCITY(),
        };

        double GRAVITY() => 1.0;
        double CONTROL_FORCE() => 2.0;
        double HORIZONTAL_DRAG() => 2.0;
        double JUMP_SIZE() => 10.0;
        double MAX_VERTICAL_VELOCITY() => 45.0;
        double MAX_HORIZONTAL_VELOCITY() => 5.0;
    }
}
