using System;
using System.Collections;
using QuickNet;

namespace Larnix.Entities
{
    public class EntityData
    {
        public EntityID ID = EntityID.None;
        public Vec2 Position = new Vec2(0, 0);
        public float Rotation = 0f;
        public string NBT = "{}";

        public EntityData DeepCopy()
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
                Position = new Vec2(
                    EndianUnsafe.FromBytes<double>(bytes, 2 + offset),
                    EndianUnsafe.FromBytes<double>(bytes, 10 + offset)
                ),
                Rotation = EndianUnsafe.FromBytes<float>(bytes, 18 + offset),
                NBT = null,
            };
        }
    }
}
