using System;
using System.Collections;
using Larnix.Core.Vectors;
using Larnix.Core.Binary;
using Larnix.Core.Utils;

namespace Larnix.Entities.Structs
{
    public class EntityData
    {
        public EntityID ID = EntityID.None;
        public Vec2 Position = new Vec2(0, 0);
        public float Rotation = 0f;
        public EntityNBT NBT = new EntityNBT();

        public EntityData DeepCopy()
        {
            return new EntityData
            {
                ID = ID,
                Position = Position,
                Rotation = Rotation,
                NBT = NBT.Copy<EntityNBT>()
            };
        }

        public byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(ID),
                Position.Serialize(),
                EndianUnsafe.GetBytes(Rotation)
                );
        }

        public static EntityData Deserialize(byte[] bytes, int offset = 0)
        {
            return new EntityData
            {
                ID = EndianUnsafe.FromBytes<EntityID>(bytes, 0 + offset),
                Position = IFixedBinary<Vec2>.Create(bytes, 8 + offset),
                Rotation = EndianUnsafe.FromBytes<float>(bytes, 18 + offset),
                NBT = null,
            };
        }
    }
}
