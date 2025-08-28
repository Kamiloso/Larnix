using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;

namespace Larnix.Server.Entities
{
    public class EntityAbstraction
    {
        // ==== Public Static Factory Methods ====
        public EntityAbstraction(string nickname) // Create player abstraction
        {
            ulong uid = (ulong)References.Server.Database.GetUserID(nickname);
            EntityData entityData = References.EntityDataManager.TryFindEntityData(uid);

            if (entityData == null)
                entityData = new EntityData // Create new player
                {
                    ID = EntityID.Player
                };

            Init(uid, entityData, nickname);
        }

        public EntityAbstraction(EntityData entityData, ulong? uid = null) // Create entity abstraction
        {
            if (entityData.ID == EntityID.Player)
                throw new System.ArgumentException("Cannot create player instance like entity!");

            ulong _uid = uid == null ? GetNextUID() : (ulong)uid;
            Init(_uid, entityData);
        }

        private void Init(ulong uid, EntityData entityData, string nickname = null)
        {
            delayed_uID = uid;
            delayed_EntityData = entityData;
            delayed_nickname = nickname;

            References.EntityDataManager.SetEntityData(delayed_uID, delayed_EntityData);
        }

        // ==== Private Things ====
        private EntityController controller = null;
        private ulong delayed_uID;
        private EntityData delayed_EntityData;
        private string delayed_nickname;

        // ==== Public Properties ====
        public bool IsActive { get { return controller != null; } }
        public ulong uID { get { return IsActive ? controller.uID : delayed_uID; } }
        public EntityData EntityData { get { return IsActive ? controller.EntityData : delayed_EntityData; } }

        // ==== Public Methods ====
        public void Activate()
        {
            if (IsActive)
                throw new System.InvalidOperationException("Entity abstraction is already active!");

            controller = EntityController.CreateRealEntityController(delayed_uID, delayed_EntityData, delayed_nickname);
        }

        public EntityController GetRealController()
        {
            return IsActive ? controller : null;
        }

        public void DeleteEntityInstant() // Execute only from after FromFixedUpdate()
        {
            References.EntityDataManager.DeleteEntityData(uID);
            if (IsActive) GameObject.Destroy(controller.gameObject);
        }

        public void UnloadEntityInstant() // Execute only from after FromFixedUpdate()
        {
            References.EntityDataManager.UnloadEntityData(uID);
            if (IsActive) GameObject.Destroy(controller.gameObject);
        }

        public void FromFixedUpdate()
        {
            if (!IsActive)
                throw new System.InvalidOperationException("Cannot execute FromFixedUpdate() on inactive entity abstraction!");

            controller.FromFixedUpdate();
        }

        // ==== Static Things ====
        private static ulong? privNextUID = null;
        private static ulong GetNextUID()
        {
            if (privNextUID != null)
            {
                ulong nextUID = (ulong)privNextUID;
                privNextUID--;
                return nextUID;
            }
            else
            {
                privNextUID = (ulong)(References.Server.Database.GetMinUID() - 1);
                return GetNextUID();
            }
        }
    }
}
