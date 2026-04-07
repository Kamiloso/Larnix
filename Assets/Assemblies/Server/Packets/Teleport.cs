#nullable enable
using Larnix.Core.Vectors;
using Larnix.Socket.Packets;
using Larnix.Core.Utils;
using Larnix.Core;

namespace Larnix.Server.Packets;

public sealed class Teleport : Payload
{
    private static int SIZE => Binary<Vec2>.Size;

    public Vec2 TargetPosition => Binary<Vec2>.Deserialize(Bytes, 0); // Binary<Vec2>.Size

    public Teleport(Vec2 targetPosition, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<Vec2>.Serialize(targetPosition)
            ), code);
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE;
    }
}
