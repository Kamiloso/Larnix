using Larnix.GameCore.Utils;
using Larnix.Core.Binary;
using Larnix.Core.Vectors;
using Larnix.Core.Misc;
using Larnix.GameCore.Enums;
using Larnix.GameCore;

namespace Larnix.Packets
{
    public struct EntityHeaderCompressed : IBinary<EntityHeaderCompressed>
    {
        public const int SIZE = sizeof(EntityID) + 2 * CompressionUtils.COMPRESSED_DOUBLE_SIZE + sizeof(byte);

        public EntityHeader Header { get; private set; }

        public EntityHeaderCompressed(EntityHeader header)
        {
            Header = header;
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

            Header = new EntityHeader(id, new Vec2(x, y), rotation);
            return true;
        }

        public readonly byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                Primitives.GetBytes(Header.ID),
                CompressionUtils.CompressWorldDouble(Header.Position.x),
                CompressionUtils.CompressWorldDouble(Header.Position.y),
                new byte[] { CompressionUtils.CompressRotation(Header.Rotation) }
            );
        }
    }
}
