using Larnix.Core.Physics;
using System;
using Larnix.Core.Vectors;
using Larnix.Entities.Structs;

namespace Larnix.Entities
{
    public class EntityServer
    {
        public readonly ulong uID;
        public readonly EntityData EntityData; // Changing doesn't need saving. This is the same object as in EntityDataManager.cs
        public EventHandler OnFrameUpdate;

        public PhysicsManager Physics { get => _Physics ?? throw new InvalidOperationException("Trying to use an uninitialized PhysicsManager."); }
        private PhysicsManager _Physics = null;

        public EntityServer(ulong uid, EntityData entityData)
        {
            uID = uid;
            EntityData = entityData;
        }

        public void InitializePhysics(PhysicsManager physics)
        {
            if (_Physics != null)
                throw new InvalidOperationException("Physics already initialized.");

            _Physics = physics;
        }

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
