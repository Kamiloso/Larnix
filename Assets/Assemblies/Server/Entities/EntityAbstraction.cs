using System.Collections;
using System.Collections.Generic;
using Larnix.Entities;
using Larnix.Entities.Structs;
using Larnix.Socket.Backend;
using Larnix.Core.Physics;
using Larnix.Core.References;
using Larnix.Core.Vectors;

namespace Larnix.Server.Entities
{
    internal class EntityAbstraction : RefObject<Server>
    {
        private EntityServer controller;
        private ulong storedUID;
        private EntityData storedEntityData;

        private UserManager UserManager => Ref<UserManager>();
        private EntityDataManager EntityDataManager => Ref<EntityDataManager>();
        private EntityManager EntityManager => Ref<EntityManager>();
        private PhysicsManager PhysicsManager => Ref<PhysicsManager>();

        public bool IsActive => controller != null;
        public ulong uID => IsActive ? controller.uID : storedUID;
        public EntityData EntityData => IsActive ? controller.EntityData : storedEntityData;

        // === Player constructor ===
        public EntityAbstraction(RefObject<Server> reff, string nickname) : base(reff)
        {
            ulong uid = (ulong)UserManager.GetUserID(nickname);
            EntityData entityData = EntityDataManager.TryFindEntityData(uid);

            if (entityData == null)
            {
                entityData = new EntityData(
                    id: EntityID.Player,
                    position: new Vec2(0.0, 0.0),
                    rotation: 0.0f
                );
            }

            Initialize(uid, entityData);
        }

        // === Entity constructor ===
        public EntityAbstraction(RefObject<Server> reff, EntityData entityData, ulong? uid = null) : base(reff)
        {
            if (entityData.ID == EntityID.Player)
                throw new System.ArgumentException("Cannot create player instance as a generic entity!");

            ulong resolvedUid = uid ?? EntityManager.GetNextUID();
            Initialize(resolvedUid, entityData);
        }

        private void Initialize(ulong uid, EntityData entityData)
        {
            storedUID = uid;
            storedEntityData = entityData;

            // flushing data to EntityDataManager
            EntityDataManager.SetEntityData(storedUID, storedEntityData);
        }

        public void Activate()
        {
            if (IsActive)
                throw new System.InvalidOperationException("Entity abstraction is already active!");

            controller = EntityFactory.ConstructEntityObject(storedUID, storedEntityData, PhysicsManager);
        }

        public EntityServer GetRealController()
        {
            return controller;
        }

        public void DeleteEntityInstant()
        {
            EntityDataManager.DeleteEntityData(uID);
        }

        public void UnloadEntityInstant()
        {
            EntityDataManager.UnloadEntityData(uID);
        }

        public void FromFrameUpdate()
        {
            if (!IsActive)
                throw new System.InvalidOperationException("Cannot execute FromFrameUpdate() on inactive entity abstraction!");

            controller.FromFrameUpdate();
        }
    }
}
