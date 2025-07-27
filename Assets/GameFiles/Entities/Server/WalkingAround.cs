using Larnix.Server;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Server.Entities;

namespace Larnix.Entities.Server
{
    public class WalkingAround : MonoBehaviour
    {
        EntityController controller;

        private void Awake()
        {
            controller = GetComponent<EntityController>();
        }

        public void DoFixedUpdate()
        {
            Vector2 position = controller.EntityData.Position;
            float rotation = controller.EntityData.Rotation;

            rotation += 2f;
            position += 0.04f * new Vector2(Mathf.Cos(rotation * Mathf.Deg2Rad), Mathf.Sin(rotation * Mathf.Deg2Rad));

            EntityData entityData = controller.EntityData.ShallowCopy();
            entityData.Position = position;
            entityData.Rotation = rotation;

            controller.UpdateEntityData(entityData);
        }
    }
}
