using Larnix.Model.Utils;
using Larnix.Core.Binary;
using Larnix.Core.Vectors;
using Larnix.Model.Entities.Structs;
using Larnix.Model.Entities;
using Larnix.Core.Utils;

namespace Larnix.Server.Packets.Structs;

public readonly struct EntityHeaderCompressed : IBinary<EntityHeaderCompressed>
{
    public const int SIZE = sizeof(EntityID) + 2 * CompressionUtils.COMPRESSED_DOUBLE_SIZE + sizeof(byte);

    public EntityHeader Header { get; }

    public EntityHeaderCompressed(EntityHeader header)
    {
        Header = header;
    }

    public bool Deserialize(byte[] bytes, int offset, out EntityHeaderCompressed result)
    {
        if (offset < 0 || offset + SIZE > bytes.Length)
        {
            result = default;
            return false;
        }

        EntityID id = Primitives.FromBytes<EntityID>(bytes, offset);
        offset += sizeof(EntityID);

        double x = CompressionUtils.DecompressWorldDouble(bytes, offset);
        offset += CompressionUtils.COMPRESSED_DOUBLE_SIZE;

        double y = CompressionUtils.DecompressWorldDouble(bytes, offset);
        offset += CompressionUtils.COMPRESSED_DOUBLE_SIZE;

        float rotation = CompressionUtils.DecompressRotation(bytes[offset]);
        offset += sizeof(byte);

        result = new EntityHeaderCompressed(new EntityHeader(id, new Vec2(x, y), rotation));
        return true;
    }

    public byte[] Serialize()
    {
        return ArrayUtils.MegaConcat(
            Primitives.GetBytes(Header.ID),
            CompressionUtils.CompressWorldDouble(Header.Position.x),
            CompressionUtils.CompressWorldDouble(Header.Position.y),
            new byte[] { CompressionUtils.CompressRotation(Header.Rotation) }
        );
    }
}
