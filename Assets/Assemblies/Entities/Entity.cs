using Larnix.Core.Physics;
using System;
using Larnix.Core.Vectors;
using Larnix.Entities.Structs;
using Larnix.Entities.All;

namespace Larnix.Entities
{
    public class Entity
    {
        public ulong UID { get; private set; }
        public EntityData EntityData { get; private set; } // connected to entity-saving system
        public PhysicsManager Physics { get; private set; }
        
        private bool _constructed = false;

        internal Entity() {}
        public record EntityInits(ulong UID, EntityData EntityData, PhysicsManager Physics);
        internal void Construct(EntityInits entityInits)
        {
            if (!_constructed)
            {
                UID = entityInits.UID;
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
}
