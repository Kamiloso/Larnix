using Larnix.Core.Binary;
using Larnix.Core.Misc;
using Larnix.Core.Vectors;
using Larnix.GameCore.Enums;

namespace Larnix.GameCore.Structs
{
    public readonly struct EntityHeader : IBinary<EntityHeader>
    {
        private const int SIZE = sizeof(EntityID) + Vec2.SIZE + sizeof(float);

        public EntityID ID { get; }
        public Vec2 Position { get; }
        public float Rotation { get; }

        public EntityHeader(EntityID id, Vec2 position, float rotation)
        {
            ID = id;
            Position = position;
            Rotation = rotation;
        }

        public bool Deserialize(byte[] data, int offset, out EntityHeader result)
        {
            if (offset < 0 || offset + SIZE > data.Length)
            {
                result = default;
                return false;
            }

            EntityID id = Primitives.FromBytes<EntityID>(data, offset);
            offset += sizeof(EntityID);

            Vec2 position = Structures.FromBytes<Vec2>(data, offset);
            offset += Vec2.SIZE;

            float rotation = Primitives.FromBytes<float>(data, offset);
            offset += sizeof(float);

            result = new EntityHeader(id, position, rotation);
            return true;
        }

        public byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                Primitives.GetBytes(ID),
                Structures.GetBytes(Position),
                Primitives.GetBytes(Rotation)
            );
        }

        public override string ToString()
        {
            return ID.ToString();
        }
    }
}
