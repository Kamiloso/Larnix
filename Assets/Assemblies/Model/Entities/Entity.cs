using System;
using Larnix.Core.Vectors;
using Larnix.Model.Entities.Structs;
using Larnix.Model.Entities.All;
using IPhysics = Larnix.Model.Physics.IPhysics;

namespace Larnix.Model.Entities;

public record EntityInits(
    ulong Uid,
    EntityData EntityData,
    EntityInterfaces Interfaces
    );

public record EntityInterfaces(
    IPhysics Physics,
    ICmdExecutor CmdExecutor
    );

public class Entity
{
    public ulong Uid { get; private set; }
    public EntityData EntityData { get; private set; } // connected to entity-saving system
    public EntityInterfaces Interfaces { get; private set; }

    private bool _constructed = false;

    internal Entity() {}
    internal void Construct(EntityInits entityInits)
    {
        if (!_constructed)
        {
            Uid = entityInits.Uid;
            EntityData = entityInits.EntityData; // should consume a given object
            Interfaces = entityInits.Interfaces;

            _constructed = true;
        }
        else throw new InvalidOperationException("Entity already initialized.");
    }

    public EventHandler OnFrameUpdate;

    public void SetTransform(Vec2 position, float rotation)
    {
        if (this is IManagesTransform iface)
            iface.ApplyTransformToSystem(position, rotation);

        EntityData.Position = position;
        EntityData.Rotation = rotation;
    }

    public void FrameUpdate()
    {
        OnFrameUpdate?.Invoke(null, EventArgs.Empty);
    }
}
