using Larnix.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;

namespace Larnix.Server.Entities
{
    public class EntityController
    {
        // -------------------------------------------------------------------------------------------
        //  WARNING: This class should only be used under the abstraction of EntityAbstraction class!
        // -------------------------------------------------------------------------------------------

        public ulong uID { get; private set; }
        public EntityData EntityData { get; private set; }

        public EntityController(ulong uid, EntityData entityData, string nickname = null)
        {
            uID = uid;
            UpdateEntityData(entityData);
        }

        public void UpdateEntityData(EntityData entityData) // Also updates physical position!
        {
            EntityData = entityData;
            Ref.EntityDataManager.SetEntityData(uID, entityData);
        }

        public void ApplyTransform()
        {
            EntityData entityData = EntityData.DeepCopy();
            UpdateEntityData(entityData);
        }

        public void FromFixedUpdate()
        {
            // not implemented
        }
    }
}
