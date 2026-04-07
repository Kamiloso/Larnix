using System;
using Larnix.Model.Utils;
using Larnix.Core.Utils;
using Larnix.Core;

namespace Larnix.Socket.Packets.Control;

public sealed class AllowConnection : Payload
{
    private const int SIZE = 32 + 64 + 32 + 8 + 8 + 8 + 8;

    public String32 Nickname => Binary<String32>.Deserialize(Bytes, 0); // 32B
    internal String64 Password => Binary<String64>.Deserialize(Bytes, 32); // 64B
    internal byte[] KeyAES => new Span<byte>(Bytes, 96, 32).ToArray(); // 32B = constant AES key size
    internal long ServerSecret => Binary<long>.Deserialize(Bytes, 128); // 8B
    internal long ChallengeID => Binary<long>.Deserialize(Bytes, 136); // 8B
    internal long Timestamp => Binary<long>.Deserialize(Bytes, 144); // 8B
    internal long RunID => Binary<long>.Deserialize(Bytes, 152); // 8B

    internal AllowConnection(in String32 nickname, in String64 password, byte[] keyAES, long serverSecret, long challengeID, long timestamp, long runID, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<String32>.Serialize(nickname),
            Binary<String64>.Serialize(password),
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
