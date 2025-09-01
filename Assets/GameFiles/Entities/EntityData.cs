using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using QuickNet;

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

        public byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(ID),
                EndianUnsafe.GetBytes(Position.x),
                EndianUnsafe.GetBytes(Position.y),
                EndianUnsafe.GetBytes(Rotation)
                );
        }

        public static EntityData Deserialize(byte[] bytes, int offset = 0)
        {
            return new EntityData
            {
                ID = EndianUnsafe.FromBytes<EntityID>(bytes, 0 + offset),
                Position = new Vector2(
                    EndianUnsafe.FromBytes<float>(bytes, 2 + offset),
                    EndianUnsafe.FromBytes<float>(bytes, 6 + offset)
                ),
                Rotation = EndianUnsafe.FromBytes<float>(bytes, 10 + offset),
                NBT = null,
            };
        }
    }
}
