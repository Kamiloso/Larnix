using System.Collections;
using System.Collections.Generic;
using Larnix.Entities;
using Larnix.Entities.Structs;
using Larnix.Socket.Backend;
using Larnix.Core.Physics;
using Larnix.Core.References;
using Larnix.Core.Vectors;
using System;
using Larnix.Core.Utils;

namespace Larnix.Server.Entities
{
    internal class EntityAbstraction : RefObject<Server>
    {
        private UserManager UserManager => Ref<UserManager>();
        private EntityDataManager EntityDataManager => Ref<EntityDataManager>();
        private EntityManager EntityManager => Ref<EntityManager>();
        private PhysicsManager PhysicsManager => Ref<PhysicsManager>();

        private EntityServer _controller;
        private ulong _storedUID;
        private EntityData _storedEntityData;

        public bool IsActive => _controller != null;
        public ulong uID => IsActive ? _controller.uID : _storedUID;
        public EntityData EntityData => IsActive ? _controller.EntityData : _storedEntityData;

        public static EntityAbstraction CreatePlayerController(RefObject<Server> reff, string nickname) =>
            new EntityAbstraction(reff, nickname);
            
        public static EntityAbstraction CreateEntityController(RefObject<Server> reff, EntityData entityData, ulong? uid = null) =>
            new EntityAbstraction(reff, entityData, uid);

        private EntityAbstraction(RefObject<Server> reff, string nickname) : base(reff)
        {
            ulong uid = (ulong)UserManager.GetUserID(nickname);
            EntityData entityData = EntityDataManager.TryFindEntityData(uid);

            if (entityData == null)
            {
                entityData = new EntityData(
                    id: EntityID.Player,
                    position: new Vec2(0, 0),
                    rotation: 0.0f
                );
            }

            entityData.Position += Common.UP_EPSILON;

            Initialize(uid, entityData);
        }

        private EntityAbstraction(RefObject<Server> reff, EntityData entityData, ulong? uid = null) : base(reff)
        {
            if (entityData.ID == EntityID.Player)
                throw new ArgumentException("Cannot create player instance as a generic entity!");

            entityData.Position += Common.UP_EPSILON;

            ulong uid2 = uid ?? EntityManager.GetNextUID();
            Initialize(uid2, entityData);
        }

        private void Initialize(ulong uid, EntityData entityData)
        {
            _storedUID = uid;
            _storedEntityData = entityData;

            // flushing data to EntityDataManager
            EntityDataManager.SetEntityData(_storedUID, _storedEntityData);
        }

        public void Activate()
        {
            if (IsActive)
                throw new InvalidOperationException("Entity abstraction is already active!");

            _controller = EntityFactory.ConstructEntityObject(_storedUID, _storedEntityData, PhysicsManager);
        }

        public EntityServer GetRealController()
        {
            return _controller;
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
                throw new InvalidOperationException("Cannot execute FromFrameUpdate() on inactive entity abstraction!");

            _controller.FromFrameUpdate();
        }
    }
}
