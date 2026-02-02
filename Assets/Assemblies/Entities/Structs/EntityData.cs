using System;
using System.Collections;
using Larnix.Core.Vectors;
using Larnix.Core.Binary;
using Larnix.Core.Utils;
using Larnix.Core.Json;

namespace Larnix.Entities.Structs
{
    public class EntityData : IBinary<EntityData>
    {
        public const int SIZE = sizeof(EntityID) + Vec2.SIZE + sizeof(float);
        
        public EntityID ID { get; private set; }
        public Vec2 Position { get; set; }
        public float Rotation { get; set; }
        public Storage Data { get; private set; }

        public EntityData() => Data = new();
        public EntityData(EntityID id, Vec2 position, float rotation, Storage data = null)
        {
            ID = id;
            Position = position;
            Rotation = rotation;
            Data = data ?? new();
        }

        public byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                Primitives.GetBytes(ID),
                Structures.GetBytes(Position),
                Primitives.GetBytes(Rotation)
                );
        }

        public bool Deserialize(byte[] bytes, int offset = 0)
        {
            if (offset + SIZE > bytes.Length)
                return false;

            ID = Primitives.FromBytes<EntityID>(bytes, offset);
            offset += sizeof(EntityID);

            Position = Structures.FromBytes<Vec2>(bytes, offset);
            offset += Vec2.SIZE;

            Rotation = Primitives.FromBytes<float>(bytes, offset);
            offset += sizeof(float);

            return true;
        }

        public EntityData DeepCopy()
        {
            return new EntityData
            {
                ID = ID,
                Position = Position,
                Rotation = Rotation,
                Data = Data.DeepCopy(),
            };
        }
    }
}
