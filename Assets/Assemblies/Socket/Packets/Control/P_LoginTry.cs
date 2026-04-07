#nullable enable
using Larnix.Model.Utils;
using Larnix.Core.Utils;
using Larnix.Core;

namespace Larnix.Socket.Packets.Control;

internal sealed class P_LoginTry : Payload
{
    private const int SIZE = 32 + 64 + 64 + 8 + 8 + 8 + 8;

    public String32 Nickname => Binary<String32>.Deserialize(Bytes, 0); // 32B
    public String64 Password => Binary<String64>.Deserialize(Bytes, 32); // 64B
    public String64 NewPassword => Binary<String64>.Deserialize(Bytes, 96); // 64B
    public long ServerSecret => Binary<long>.Deserialize(Bytes, 160); // 8B
    public long ChallengeID => Binary<long>.Deserialize(Bytes, 168); // 8B
    public long Timestamp => Binary<long>.Deserialize(Bytes, 176); // 8B
    public long RunID => Binary<long>.Deserialize(Bytes, 184); // 8B

    public P_LoginTry(in String32 nickname, in String64 password, long serverSecret,
        long challengeID, long timestamp, long runID, in String64? newPassword = null, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<String32>.Serialize(nickname),
            Binary<String64>.Serialize(password),
            Binary<String64>.Serialize(newPassword ?? password),
            Binary<long>.Serialize(serverSecret),
            Binary<long>.Serialize(challengeID),
            Binary<long>.Serialize(timestamp),
            Binary<long>.Serialize(runID)
            ), code);
    }

    public bool IsPasswordChange()
    {
        return Password != NewPassword;
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE &&
            Validation.IsGoodNickname(Nickname) &&
            Validation.IsGoodPassword(Password) &&
            Validation.IsGoodPassword(NewPassword);
    }
}
