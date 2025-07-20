using Larnix.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using Larnix.Entities.Server;

namespace Larnix.Server
{
    public class EntityController : MonoBehaviour
    {
        public static EntityController CreatePlayerController(string nickname)
        {
            ulong uid = (ulong)References.Server.Database.GetUserID(nickname);
            EntityData entityData = References.EntityDataManager.TryFindEntityData(uid);

            if (entityData == null)
                entityData = new EntityData // Create new player
                {
                    ID = EntityID.Player
                };

            GameObject gobj = Prefabs.CreateEntity(entityData.ID, Prefabs.Mode.Server);
            gobj.name = "Player (" + nickname + ") [" + uid + "]";
            gobj.transform.SetParent(References.EntityDataManager.transform, false);
            EntityController controller = gobj.GetComponent<EntityController>();

            controller.Initialize(uid, entityData);
            return controller;
        }

        public static EntityController CreateNewEntityController(EntityData entityData)
        {
            ulong uid = Common.GetRandomUID(); // It is supposed to be unique
            return CreateExistingEntityController(uid, entityData);
        }

        public static EntityController CreateExistingEntityController(ulong uid, EntityData entityData)
        {
            GameObject gobj = Prefabs.CreateEntity(entityData.ID, Prefabs.Mode.Server);
            gobj.name = entityData.ID.ToString() + " [" + uid + "]";
            gobj.transform.SetParent(References.EntityDataManager.transform, false);
            EntityController controller = gobj.GetComponent<EntityController>();

            controller.Initialize(uid, entityData);
            return controller;
        }

        public ulong uID { get; private set; }
        public EntityData EntityData { get; private set; }

        private void Initialize(ulong uid, EntityData entityData)
        {
            uID = uid;
            UpdateEntityData(entityData);
            gameObject.SetActive(false);
        }

        public void ActivateIfNotActive()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        public void UpdateEntityData(EntityData entityData) // Also updates physical position!
        {
            EntityData = entityData;
            transform.position = entityData.Position;
            References.EntityDataManager.SetEntityData(uID, entityData);
        }

        public void DeleteEntityInstant() // Execute only from after FixedUpdate
        {
            References.EntityDataManager.DeleteEntityData(uID);
            Destroy(gameObject);
        }

        public void UnloadEntityInstant() // Execute only from after FixedUpdate
        {
            References.EntityDataManager.UnloadEntityData(uID);
            Destroy(gameObject);
        }

        public void FromFixedUpdate()
        {
            // Walking around
            WalkingAround WalkingAround = GetComponent<WalkingAround>();
            if (WalkingAround != null) WalkingAround.DoFixedUpdate();
        }
    }
}
