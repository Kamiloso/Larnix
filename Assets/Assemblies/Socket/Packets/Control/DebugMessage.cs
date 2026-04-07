#nullable enable
using Larnix.Model.Utils;
using Larnix.Core.Utils;
using Larnix.Core;

namespace Larnix.Socket.Packets.Control;

public sealed class DebugMessage : Payload
{
    private const int SIZE = 512;
    public String512 Message => Binary<String512>.Deserialize(Bytes, 0);

    public DebugMessage(in String512 message, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<String512>.Serialize(message)
            ), code);
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE &&
            Validation.IsGoodText<String512>(Message);
    }
}
