using System;
using Larnix.Model.Utils;
using Larnix.Core.Utils;
using Larnix.Core.Serialization;

namespace Larnix.Socket.Packets.Control;

public sealed class AllowConnection : Payload_Legacy
{
    private const int SIZE = 34 + 66 + 32 + 8 + 8 + 8 + 8;

    public FixedString32 Nickname => Binary<FixedString32>.Deserialize(Bytes, 0); // 34B
    internal FixedString64 Password => Binary<FixedString64>.Deserialize(Bytes, 34); // 66B
    internal byte[] KeyAES => new Span<byte>(Bytes, 100, 32).ToArray(); // 32B = constant AES key size
    internal long ServerSecret => Binary<long>.Deserialize(Bytes, 132); // 8B
    internal long ChallengeID => Binary<long>.Deserialize(Bytes, 140); // 8B
    internal long Timestamp => Binary<long>.Deserialize(Bytes, 148); // 8B
    internal long RunID => Binary<long>.Deserialize(Bytes, 156); // 8B
    internal AllowConnection(in FixedString32 nickname, in FixedString64 password, byte[] keyAES, long serverSecret, long challengeID, long timestamp, long runID, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<FixedString32>.Serialize(nickname),
            Binary<FixedString64>.Serialize(password),
            keyAES?.Length == 32 ? keyAES : throw new ArgumentException("KeyAES must have length of exactly 32 bytes."),
            Binary<long>.Serialize(serverSecret),
            Binary<long>.Serialize(challengeID),
            Binary<long>.Serialize(timestamp),
            Binary<long>.Serialize(runID)
            ), code);
    }

    internal P_LoginTry ToLoginTry()
    {
        return new P_LoginTry(
            nickname: Nickname,
            password: Password,
            serverSecret: ServerSecret,
            challengeID: ChallengeID,
            timestamp: Timestamp,
            runID: RunID
            );
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE &&
            Validation.IsGoodNickname(Nickname) &&
            Validation.IsGoodPassword(Password);
    }
}
