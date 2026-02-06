using Larnix.Entities.Structs;
using Larnix.Core.Utils;
using Larnix.Core.Binary;
using Larnix.Entities;
using Larnix.Core.Vectors;

namespace Larnix.Packets
{
    public class EntityDataCompressed : IBinary<EntityDataCompressed>
    {
        public const int SIZE = sizeof(EntityID) + CompressionUtils.COMPRESSED_DOUBLE_SIZE + CompressionUtils.COMPRESSED_DOUBLE_SIZE + sizeof(byte);

        public EntityData Contents { get; private set; }

        public EntityDataCompressed() => Contents = new();
        public EntityDataCompressed(EntityData contents) => Contents = contents ?? new();

        public byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                Primitives.GetBytes(Contents.ID),
                CompressionUtils.CompressWorldDouble(Contents.Position.x),
                CompressionUtils.CompressWorldDouble(Contents.Position.y),
                new byte[] { CompressionUtils.CompressRotation(Contents.Rotation) }
            );
        }

        public bool Deserialize(byte[] bytes, int offset = 0)
        {
            if (offset + SIZE > bytes.Length)
                return false;

            EntityID id = Primitives.FromBytes<EntityID>(bytes, offset);
            offset += sizeof(EntityID);

            double x = CompressionUtils.DecompressWorldDouble(bytes, offset);
            offset += CompressionUtils.COMPRESSED_DOUBLE_SIZE;

            double y = CompressionUtils.DecompressWorldDouble(bytes, offset);
            offset += CompressionUtils.COMPRESSED_DOUBLE_SIZE;

            float rotation = CompressionUtils.DecompressRotation(bytes[offset]);
            offset += sizeof(byte);

            Contents = new EntityData(id, new Vec2(x, y), rotation);
            return true;
        }
    }
}
