#nullable enable
using Larnix.Model.Utils;
using Larnix.Core.Utils;
using Larnix.Core.Serialization;

namespace Larnix.Socket.Packets.Control;

public sealed class DebugMessage : Payload_Legacy
{
    private const int SIZE = 514;
    public FixedString512 Message => Binary<FixedString512>.Deserialize(Bytes, 0);

    public DebugMessage(in FixedString512 message, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<FixedString512>.Serialize(message)
            ), code);
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE &&
            Validation.IsGoodText<FixedString512>(Message);
    }
}
