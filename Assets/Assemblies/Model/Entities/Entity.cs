using System;
using Larnix.Core.Vectors;
using Larnix.Model.Entities.Structs;
using Larnix.Model.Entities.All;
using Larnix.Model.Interfaces;

namespace Larnix.Model.Entities;

public class Entity
{
    public ulong Uid { get; private set; }
    public EntityData EntityData { get; private set; } // connected to entity-saving system
    public IPhysicsManager Physics { get; private set; }

    private bool _constructed = false;

    internal Entity() {}
    public record EntityInits(ulong Uid, EntityData EntityData, IPhysicsManager Physics);
    internal void Construct(EntityInits entityInits)
    {
        if (!_constructed)
        {
            Uid = entityInits.Uid;
            EntityData = entityInits.EntityData; // should consume a given object
            Physics = entityInits.Physics;

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
