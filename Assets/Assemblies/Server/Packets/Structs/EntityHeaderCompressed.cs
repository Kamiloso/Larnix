using Larnix.Model.Utils;
using Larnix.Core.Vectors;
using Larnix.Model.Entities.Structs;
using Larnix.Model.Entities;
using Larnix.Core.Utils;
using Larnix.Core;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct EntityHeaderCompressed : IFixedStruct<EntityHeaderCompressed>
{
    private readonly long _d1; // 0 - 7
    private readonly int _d2; // 8 - 11
    private readonly byte _d3; // 12 - 12

    public EntityHeader Header
    {
        get
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                Binary<long>.Serialize(_d1),
                Binary<int>.Serialize(_d2),
                Binary<byte>.Serialize(_d3)
            );

            EntityID id = Binary<EntityID>.Deserialize(bytes, 0);
            double x = CompressionUtils.DecompressWorldDouble(bytes, 2);
            double y = CompressionUtils.DecompressWorldDouble(bytes, 7);
            float rotation = CompressionUtils.DecompressRotation(bytes[12]);

            return new EntityHeader(id, new Vec2(x, y), rotation);
        }
    }

    public EntityHeaderCompressed(EntityHeader header)
    {
        header = header.Sanitize();

        byte[] bytes = ArrayUtils.MegaConcat(
            Binary<EntityID>.Serialize(header.ID),
            CompressionUtils.CompressWorldDouble(header.Position.x),
            CompressionUtils.CompressWorldDouble(header.Position.y),
            new byte[] { CompressionUtils.CompressRotation(header.Rotation) }
        );

        _d1 = Binary<long>.Deserialize(bytes, 0);
        _d2 = Binary<int>.Deserialize(bytes, 8);
        _d3 = Binary<byte>.Deserialize(bytes, 12);
    }
}
