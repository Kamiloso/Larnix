using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;

namespace Larnix.Server
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

        public EntityAbstraction(EntityData entityData) // Create new entity abstraction
        {
            ulong uid = Common.GetRandomUID(); // It is supposed to be unique
            Init(uid, entityData);
        }

        public EntityAbstraction(ulong uid, EntityData entityData) // Create existing entity abstraction
        {
            Init(uid, entityData);
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

            controller = EntityController.ManualCreateEntityController(delayed_uID, delayed_EntityData, delayed_nickname);
        }

        public void DeleteEntityInstant()
        {
            if(IsActive)
            {
                controller.DeleteEntityInstant();
            }
            else
            {

            }
        }

        public void UnloadEntityInstant()
        {
            if(IsActive)
            {
                controller.UnloadEntityInstant();
            }
            else
            {

            }
        }

        public void FromFixedUpdate()
        {
            if (!IsActive)
                throw new System.InvalidOperationException("Cannot execute FromFixedUpdate() on inactive entity abstraction!");

            controller.FromFixedUpdate();
        }
    }
}
