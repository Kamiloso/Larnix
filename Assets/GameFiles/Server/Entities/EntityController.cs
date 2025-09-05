using Larnix.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using Larnix.Entities.Server;

namespace Larnix.Server.Entities
{
    public class EntityController : MonoBehaviour
    {
        // -------------------------------------------------------------------------------------------
        //  WARNING: This class should only be used under the abstraction of EntityAbstraction class!
        // -------------------------------------------------------------------------------------------

        public ulong uID { get; private set; }
        public EntityData EntityData { get; private set; }

        public static EntityController CreateRealEntityController(ulong uid, EntityData entityData, string nickname = null)
        {
            GameObject gobj = Resources.CreateEntity(entityData.ID, Resources.Mode.Server);
            gobj.name = entityData.ID.ToString() + (nickname == null ? "" : $" ({nickname})") + " [" + uid + "]";
            gobj.transform.SetParent(Ref.EntityDataManager.transform, false);
            EntityController controller = gobj.GetComponent<EntityController>();

            controller.uID = uid;
            controller.UpdateEntityData(entityData);

            return controller;
        }

        public void UpdateEntityData(EntityData entityData) // Also updates physical position!
        {
            EntityData = entityData;
            transform.position = entityData.Position;
            Ref.EntityDataManager.SetEntityData(uID, entityData);
        }

        public void ApplyTransform()
        {
            EntityData entityData = EntityData.DeepCopy();
            entityData.Position = transform.position;
            UpdateEntityData(entityData);
        }

        public void UpdateRotation(float rotation)
        {
            EntityData entityData = EntityData.DeepCopy();
            entityData.Rotation = rotation;
            UpdateEntityData(entityData);
        }

        public void FromFixedUpdate()
        {
            // Walking around
            WalkingAround WalkingAround = GetComponent<WalkingAround>();
            if (WalkingAround != null) WalkingAround.DoFixedUpdate();
        }
    }
}
