using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Larnix.Entities
{
    public class EntityData
    {
        public EntityID ID = EntityID.None;
        public Vector2 Position = new Vector2(0f, 0f);
        public float Rotation = 0f;
        public string NBT = "{}";

        public EntityData ShallowCopy()
        {
            return new EntityData
            {
                ID = ID,
                Position = Position,
                Rotation = Rotation,
                NBT = NBT
            };
        }

        public byte[] SerializeTransform()
        {
            return ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(ID),
                EndianUnsafe.GetBytes(Position.x),
                EndianUnsafe.GetBytes(Position.y),
                EndianUnsafe.GetBytes(Rotation)
                );
        }

        public void DeserializeTransform(byte[] bytes)
        {
            ID = EndianUnsafe.FromBytes<EntityID>(bytes, 0);
            Position = new Vector2(
                EndianUnsafe.FromBytes<float>(bytes, 2),
                EndianUnsafe.FromBytes<float>(bytes, 6)
                );
            Rotation = EndianUnsafe.FromBytes<float>(bytes, 10);
            NBT = null;
        }
    }
}
