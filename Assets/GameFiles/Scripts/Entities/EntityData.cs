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
            byte[] bytes1 = BitConverter.GetBytes((ushort)ID);
            byte[] bytes2 = BitConverter.GetBytes(Position.x);
            byte[] bytes3 = BitConverter.GetBytes(Position.y);
            byte[] bytes4 = BitConverter.GetBytes(Rotation);

            return bytes1.Concat(bytes2).Concat(bytes3).Concat(bytes4).ToArray();
        }

        public void DeserializeTransform(byte[] bytes)
        {
            ID = (EntityID)BitConverter.ToUInt16(bytes, 0);
            Position = new Vector2(
                BitConverter.ToSingle(bytes, 2),
                BitConverter.ToSingle(bytes, 6)
                );
            Rotation = BitConverter.ToSingle(bytes, 10);
            NBT = null;
        }

        public enum EntityID : ushort
        {
            None,
            Player,
        }
    }
}
