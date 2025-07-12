using Larnix.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;

namespace Larnix.Server
{
    public class EntityController : MonoBehaviour
    {
        public static EntityController CreatePlayerController(string nickname)
        {
            ulong uid = (ulong)References.Server.Database.GetUserID(nickname);
            EntityData entityData = References.EntityDataManager.TryFindEntityData(uid);

            GameObject gobj = new GameObject();
            gobj.name = "Player (" + nickname + ") [" + uid + "]";
            gobj.transform.SetParent(References.EntityDataManager.transform, false);
            EntityController controller = gobj.AddComponent<EntityController>();

            if (entityData == null)
                entityData = new EntityData // Create new player
                {
                    ID = EntityData.EntityID.Player
                };

            controller.Initialize(uid, entityData);
            return controller;
        }

        public static EntityController SummonEntity(EntityData entityData)
        {
            ulong uid = Common.GetRandomUID(); // It is supposed to be unique
            
            GameObject gobj = new GameObject();
            gobj.name = entityData.ID.ToString() + " [" + uid + "]";
            gobj.transform.SetParent(References.EntityDataManager.transform, false);
            EntityController controller = gobj.AddComponent<EntityController>();

            controller.Initialize(uid, entityData);
            return controller;
        }

        public ulong uID { get; private set; }
        public EntityData EntityData;

        private void Initialize(ulong uid, EntityData entityData)
        {
            uID = uid;
            EntityData = entityData;
            gameObject.SetActive(false);
        }

        public void ActivateIfNotActive()
        {
            if(!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        private void Update()
        {
            References.EntityDataManager.SetEntityData(uID, EntityData);
        }

        public void UnloadEntity()
        {
            References.EntityDataManager.UnloadEntityData(uID);
            Destroy(gameObject);
        }

        public void RemoveEntity()
        {
            References.EntityDataManager.RemoveEntityData(uID);
            Destroy(gameObject);
        }
    }
}
