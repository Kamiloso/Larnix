using Larnix.Core.Physics;
using System;
using Larnix.Core.Vectors;
using Larnix.Entities.Structs;
using Larnix.Entities.All;

namespace Larnix.Entities
{
    public class EntityServer
    {
        public ulong uID { get; init; }
        public EntityData EntityData { get; init; } // connected to entity-saving system

        internal EntityServer(ulong uid, EntityData entityData)
        {
            uID = uid;
            EntityData = entityData; // should consume a given object
        }

        private PhysicsManager _physics = null;
        internal PhysicsManager Physics
        {
            set => _physics = _physics == null ? value : throw new InvalidOperationException("Physics already initialized.");
            get => _physics ?? throw new InvalidOperationException("Trying to use an uninitialized PhysicsManager.");
        }

        public EventHandler OnFrameUpdate;

        public void SetTransform(Vec2 position, float rotation)
        {
            if (this is IManagesTransform iface)
                iface.ApplyTransformToSystem(position, rotation);

            EntityData.Position = position;
            EntityData.Rotation = rotation;
        }

        public void FromFrameUpdate()
        {
            OnFrameUpdate?.Invoke(null, EventArgs.Empty);
        }
    }
}
