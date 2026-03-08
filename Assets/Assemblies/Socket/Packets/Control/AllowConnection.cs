using System;
using Larnix.GameCore.Utils;
using Larnix.Core.Binary;
using Larnix.Core.Misc;

namespace Larnix.Socket.Packets.Control
{
    public sealed class AllowConnection : Payload
    {
        private const int SIZE = 32 + 64 + 32 + 8 + 8 + 8 + 8;

        public String32 Nickname => Primitives.FromBytes<String32>(Bytes, 0); // 32B
        internal String64 Password => Primitives.FromBytes<String64>(Bytes, 32); // 64B
        internal byte[] KeyAES => new Span<byte>(Bytes, 96, 32).ToArray(); // 32B = constant AES key size
        internal long ServerSecret => Primitives.FromBytes<long>(Bytes, 128); // 8B
        internal long ChallengeID => Primitives.FromBytes<long>(Bytes, 136); // 8B
        internal long Timestamp => Primitives.FromBytes<long>(Bytes, 144); // 8B
        internal long RunID => Primitives.FromBytes<long>(Bytes, 152); // 8B

        internal AllowConnection(in String32 nickname, in String64 password, byte[] keyAES, long serverSecret, long challengeID, long timestamp, long runID, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                Primitives.GetBytes(nickname),
                Primitives.GetBytes(password),
                keyAES?.Length == 32 ? keyAES : throw new ArgumentException("KeyAES must have length of exactly 32 bytes."),
                Primitives.GetBytes(serverSecret),
                Primitives.GetBytes(challengeID),
                Primitives.GetBytes(timestamp),
                Primitives.GetBytes(runID)
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
}