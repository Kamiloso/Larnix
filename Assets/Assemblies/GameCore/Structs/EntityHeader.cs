using System;
using Larnix.Core.Binary;
using Larnix.Core.Misc;
using Larnix.Core.Vectors;
using Larnix.GameCore.Enums;

namespace Larnix.GameCore
{
    public struct EntityHeader : IBinary<EntityHeader>
    {
        private const int SIZE = sizeof(EntityID) + Vec2.SIZE + sizeof(float);

        public EntityID ID { get; private set; }
        public Vec2 Position { get; private set; }
        public float Rotation { get; private set; }

        public EntityHeader(EntityID id, Vec2 position, float rotation)
        {
            ID = id;
            Position = position;
            Rotation = rotation;
        }

        public bool Deserialize(byte[] data, int offset = 0)
        {
            if (offset + SIZE > data.Length)
                return false;

            ID = Primitives.FromBytes<EntityID>(data, offset);
            offset += sizeof(EntityID);

            Position = Structures.FromBytes<Vec2>(data, offset);
            offset += Vec2.SIZE;

            Rotation = Primitives.FromBytes<float>(data, offset);
            offset += sizeof(float);
            
            return true;
        }

        public readonly byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                Primitives.GetBytes(ID),
                Structures.GetBytes(Position),
                Primitives.GetBytes(Rotation)
            );
        }
    }
}
