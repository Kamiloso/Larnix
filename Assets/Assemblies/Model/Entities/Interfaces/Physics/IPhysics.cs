#nullable enable
using Larnix.Model.Physics;
using Larnix.Core.Vectors;
using Larnix.Model.Physics.Structs;

namespace Larnix.Model.Entities.All;

public interface IPhysics : IManagesTransform, IHasCollider, IPhysicsProperties
{
    DynamicCollider dynamicCollider { get; set; }
    InputData inputData { get; } // getter lazy-activates behaviour
    OutputData? outputData { get; set; } // last known output

    void Init()
    {
        This.OnFrameUpdate += (sender, args) => PhysicsUpdate();
        dynamicCollider = new DynamicCollider(
            center: This.EntityData.Position,
            offset: COLLIDER_OFFSET(),
            size: COLLIDER_SIZE(),
            properties: PHYSICS_PROPERTIES()
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

        outputData = Physics.TickPhysics(dynamicCollider, idata);
        This.EntityData.Position = ((OutputData)outputData).Position;

        // head rotation perform

        if (idata.Right && !idata.Left)
            This.EntityData.Rotation = 0f;

        if (idata.Left && !idata.Right)
            This.EntityData.Rotation = 180f;
    }
}
