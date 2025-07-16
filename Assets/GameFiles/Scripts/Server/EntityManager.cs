using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using System.Linq;

namespace Larnix.Server
{
    public class EntityManager : MonoBehaviour
    {
        private readonly Dictionary<string, EntityController> PlayerControllers = new Dictionary<string, EntityController>();
        private readonly Dictionary<ulong, EntityController> EntityControllers = new Dictionary<ulong, EntityController>();

        private void Awake()
        {
            References.EntityManager = this;
        }

        public void FromEarlyUpdate()
        {
            foreach(var controller in EntityControllers.Values)
            {
                if (controller.EntityData.ID != EntityData.EntityID.Player)
                {
                    EntityData newData = controller.EntityData.ShallowCopy();
                    newData.Position += new Vector2(Mathf.Cos(Time.frameCount * 0.06f) * 0.03f, 0f);
                    controller.UpdateEntityData(newData);
                }
            }
        }

        public void CreatePlayerController(string nickname)
        {
            EntityController playerController = EntityController.CreatePlayerController(nickname);
            PlayerControllers.Add(nickname, playerController);
            EntityControllers.Add(playerController.uID, playerController);
        }

        public EntityController GetPlayerController(string nickname)
        {
            if(PlayerControllers.ContainsKey(nickname))
                return PlayerControllers[nickname];
            return null;
        }

        public void UnloadPlayerController(string nickname)
        {
            EntityController playerController = PlayerControllers[nickname];
            PlayerControllers.Remove(nickname);
            EntityControllers.Remove(playerController.uID);
            playerController.UnloadEntityInstant();
        }

        public void SummonEntity(EntityData entityData)
        {
            EntityController entityController = EntityController.CreateNewEntityController(entityData);
            EntityControllers.Add(entityController.uID, entityController);
            entityController.ActivateIfNotActive();
        }

        public void LoadEntitiesByChunk(int[] chunkCoords)
        {
            Dictionary<ulong, EntityData> entities = References.EntityDataManager.GetUnloadedEntitiesByChunk(chunkCoords);
            foreach (var vkp in entities)
            {
                EntityController entityController = EntityController.CreateExistingEntityController(vkp.Key, vkp.Value);
                EntityControllers.Add(entityController.uID, entityController);
                entityController.ActivateIfNotActive();
            }
        }

        private void UnloadEntity(ulong uid)
        {
            if (!EntityControllers.ContainsKey(uid))
                throw new System.InvalidOperationException("Entity with ID " + uid + " is not loaded!");

            EntityController entityController = EntityControllers[uid];
            if (entityController.EntityData.ID == EntityData.EntityID.Player)
                throw new System.InvalidOperationException("Cannot unload player this way!");

            EntityControllers[uid].UnloadEntityInstant();
            EntityControllers.Remove(uid);
        }

        private void KillEntity(ulong uid)
        {
            if (!EntityControllers.ContainsKey(uid))
                throw new System.InvalidOperationException("Entity with ID " + uid + " is not loaded!");

            string nickname = PlayerControllers.FirstOrDefault(kvp => kvp.Value.uID == uid).Key;
            if(nickname != null)
                PlayerControllers.Remove(nickname);

            EntityControllers[uid].DeleteEntityInstant();
            EntityControllers.Remove(uid);
        }
    }
}
