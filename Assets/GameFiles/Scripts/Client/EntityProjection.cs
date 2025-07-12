using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;

namespace Larnix.Client
{
    public class EntityProjection : MonoBehaviour
    {
        [SerializeField] Transform Head;

        public string NBT { get; private set; } = null;

        private void Awake()
        {
            if(Head == null)
                Head = transform;
        }

        public void UpdateTransform(EntityData entityData)
        {
            transform.position = entityData.Position;
            Head.rotation = Quaternion.Euler(0f, 0f, entityData.Rotation);
        }
    }
}
