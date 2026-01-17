using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using Larnix.Entities.Structs;
using Larnix.Socket.Backend;
using Larnix.Core.Physics;
using Larnix.Server.References;

namespace Larnix.Server.Entities
{
    internal class EntityAbstraction : RefObject
    {
        // Player constructor
        public EntityAbstraction(RefObject reff, string nickname) : base(reff)
        {
            ulong uid = (ulong)Ref<QuickServer>().UserManager.GetUserID(nickname);
            EntityData entityData = Ref<EntityDataManager>().TryFindEntityData(uid);

            if (entityData == null)
            {
                entityData = new EntityData
                {
                    ID = EntityID.Player
                };
            }

            Initialize(uid, entityData);
        }

        // Entity constructor
        public EntityAbstraction(RefObject reff, EntityData entityData, ulong? uid = null) : base(reff)
        {
            if (entityData.ID == EntityID.Player)
                throw new System.ArgumentException("Cannot create player instance as a generic entity!");

            ulong resolvedUid = uid ?? Ref<EntityManager>().GetNextUID();
            Initialize(resolvedUid, entityData);
        }

        private void Initialize(ulong uid, EntityData entityData)
        {
            storedUID = uid;
            storedEntityData = entityData;

            // flushing data to EntityDataManager
            Ref<EntityDataManager>().SetEntityData(storedUID, storedEntityData);
        }

        private EntityServer controller;
        private ulong storedUID;
        private EntityData storedEntityData;

        public bool IsActive => controller != null;
        public ulong uID => IsActive ? controller.uID : storedUID;
        public EntityData EntityData => IsActive ? controller.EntityData : storedEntityData;

        public void Activate()
        {
            if (IsActive)
                throw new System.InvalidOperationException("Entity abstraction is already active!");

            controller = EntityFactory.ConstructEntityObject(storedUID, storedEntityData, Ref<PhysicsManager>());
        }

        public EntityServer GetRealController()
        {
            return controller;
        }

        public void DeleteEntityInstant()
        {
            Ref<EntityDataManager>().DeleteEntityData(uID);
        }

        public void UnloadEntityInstant()
        {
            Ref<EntityDataManager>().UnloadEntityData(uID);
        }

        public void FromFixedUpdate()
        {
            if (!IsActive)
                throw new System.InvalidOperationException("Cannot execute FromFixedUpdate() on inactive entity abstraction!");

            controller.FromFixedUpdate();
        }
    }
}
