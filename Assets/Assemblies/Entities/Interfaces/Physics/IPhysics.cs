using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Physics;

namespace Larnix.Entities
{
    public interface IPhysics : IManagesTransform, IHasCollider
    {
        PhysicsProperties PHYSICS_PROPERTIES();

        DynamicCollider dynamicCollider { get; set; }
        InputData inputData { get; }
        OutputData? outputData { get; set; }

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
            outputData = dynamicCollider.PhysicsUpdate(inputData);
            This.EntityData.Position = ((OutputData)outputData).Position;
        }
    }
}
