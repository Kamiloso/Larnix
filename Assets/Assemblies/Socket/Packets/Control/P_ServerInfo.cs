#nullable enable
using Larnix.Model.Utils;
using Larnix.Core.Utils;
using Larnix.Core.Serialization;

namespace Larnix.Socket.Packets.Control;

internal sealed class P_ServerInfo : Payload_Legacy
{
    private const int SIZE = 34;
    public FixedString32 Nickname => Binary<FixedString32>.Deserialize(Bytes, 0);

    public P_ServerInfo(in FixedString32 message, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<FixedString32>.Serialize(message)
            ), code);
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE &&
            Validation.IsGoodNickname(Nickname);
    }
}
