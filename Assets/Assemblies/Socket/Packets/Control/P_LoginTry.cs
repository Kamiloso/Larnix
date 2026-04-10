#nullable enable
using Larnix.Model.Utils;
using Larnix.Core.Utils;
using Larnix.Core.Serialization;

namespace Larnix.Socket.Packets.Control;

internal sealed class P_LoginTry : Payload_Legacy
{
    private const int SIZE = 34 + 66 + 66 + 8 + 8 + 8 + 8;

    public FixedString32 Nickname => Binary<FixedString32>.Deserialize(Bytes, 0); // 34B
    public FixedString64 Password => Binary<FixedString64>.Deserialize(Bytes, 34); // 66B
    public FixedString64 NewPassword => Binary<FixedString64>.Deserialize(Bytes, 100); // 66B
    public long ServerSecret => Binary<long>.Deserialize(Bytes, 166); // 8B
    public long ChallengeID => Binary<long>.Deserialize(Bytes, 174); // 8B
    public long Timestamp => Binary<long>.Deserialize(Bytes, 182); // 8B
    public long RunID => Binary<long>.Deserialize(Bytes, 190); // 8B

    public P_LoginTry(in FixedString32 nickname, in FixedString64 password, long serverSecret,
        long challengeID, long timestamp, long runID, in FixedString64? newPassword = null, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<FixedString32>.Serialize(nickname),
            Binary<FixedString64>.Serialize(password),
            Binary<FixedString64>.Serialize(newPassword ?? password),
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
