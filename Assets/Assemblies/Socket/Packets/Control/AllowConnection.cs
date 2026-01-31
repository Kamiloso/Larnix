using System;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Utils;
using Larnix.Core.Binary;

namespace Larnix.Socket.Packets.Control
{
    public class AllowConnection : Payload
    {
        private const int SIZE = 32 + 64 + 32 + 8 + 8 + 8 + 8;

        public String32 Nickname => EndianUnsafe.FromBytes<String32>(Bytes, 0); // 32B
        internal String64 Password => EndianUnsafe.FromBytes<String64>(Bytes, 32); // 64B
        internal byte[] KeyAES => new Span<byte>(Bytes, 96, 32).ToArray(); // 32B = constant AES key size
        internal long ServerSecret => EndianUnsafe.FromBytes<long>(Bytes, 128); // 8B
        internal long ChallengeID => EndianUnsafe.FromBytes<long>(Bytes, 136); // 8B
        internal long Timestamp => EndianUnsafe.FromBytes<long>(Bytes, 144); // 8B
        internal long RunID => EndianUnsafe.FromBytes<long>(Bytes, 152); // 8B

        public AllowConnection() { }
        internal AllowConnection(string nickname, string password, byte[] keyAES, long serverSecret, long challengeID, long timestamp, long runID, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes<String32>(nickname),
                EndianUnsafe.GetBytes<String64>(password),
                keyAES?.Length == 32 ? keyAES : throw new ArgumentException("KeyAES must have length of exactly 32 bytes."),
                EndianUnsafe.GetBytes(serverSecret),
                EndianUnsafe.GetBytes(challengeID),
                EndianUnsafe.GetBytes(timestamp),
                EndianUnsafe.GetBytes(runID)
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
            return Bytes?.Length == SIZE &&
                Validation.IsGoodNickname(Nickname) &&
                Validation.IsGoodPassword(Password);
        }
    }
}