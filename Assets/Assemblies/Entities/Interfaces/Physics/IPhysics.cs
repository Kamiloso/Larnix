using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Physics;
using Larnix.Core.Vectors;

namespace Larnix.Entities
{
    public interface IPhysics : IManagesTransform, IHasCollider, IPhysicsProperties
    {
        DynamicCollider dynamicCollider { get; set; }
        InputData inputData { get; } // getter lazy-activates behaviour
        OutputData? outputData { get; set; } // last known output

        void Init()
        {
            This.OnFixedUpdate += (sender, args) => PhysicsUpdate();
            dynamicCollider = new DynamicCollider(
                Physics,
                This.EntityData.Position,
                COLLIDER_OFFSET(),
                COLLIDER_SIZE(),
                PHYSICS_PROPERTIES()
                );
        }

        void IManagesTransform.ApplyTransformToSystem(Vec2 position, float rotation)
        {
            outputData = dynamicCollider.NoPhysicsUpdate(position);
            This.EntityData.Position = ((OutputData)outputData).Position;
        }

        private void PhysicsUpdate()
        {
            InputData idata = inputData; // getter performs actions!

            outputData = dynamicCollider.PhysicsUpdate(idata);
            This.EntityData.Position = ((OutputData)outputData).Position;

            // head rotation perform

            if (idata.Right && !idata.Left)
                This.EntityData.Rotation = 0f;

            if (idata.Left && !idata.Right)
                This.EntityData.Rotation = 180f;
        }
    }
}
