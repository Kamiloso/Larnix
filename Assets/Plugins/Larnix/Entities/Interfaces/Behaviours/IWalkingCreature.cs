using Larnix.Physics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Entities
{
    public interface IWalkingCreature : IPhysics
    {
        PhysicsProperties IPhysics.PHYSICS_PROPERTIES() => new PhysicsProperties
        {
            Gravity = 1.00,
            HorizontalForce = 2.00,
            HorizontalDrag = 2.00,
            JumpSize = JUMP_SIZE(),
            MaxVerticalVelocity = 45.00,
            MaxHorizontalVelocity = 15.00,
        };
        InputData IPhysics.inputData => new InputData
        {
            Jump = outputData?.OnRightWall == true,
            Right = true,
            Left = false,
        };

        double JUMP_SIZE();
    }
}
