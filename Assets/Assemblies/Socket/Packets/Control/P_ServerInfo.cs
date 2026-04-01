using Larnix.Model.Utils;
using Larnix.Core.Binary;
using Larnix.Core.Utils;

namespace Larnix.Socket.Packets.Control;

internal sealed class P_ServerInfo : Payload
{
    private const int SIZE = 32;
    public String32 Nickname => Primitives.FromBytes<String32>(Bytes, 0);

    public P_ServerInfo(in String32 message, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Primitives.GetBytes(message)
            ), code);
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE &&
            Validation.IsGoodNickname(Nickname);
    }
}
