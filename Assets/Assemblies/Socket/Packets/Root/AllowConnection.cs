using System;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Utils;
using Larnix.Core.Serialization;
using Larnix.Socket.Channel;

namespace Larnix.Socket.Packets
{
    public class AllowConnection : Payload
    {
        private const int SIZE = 32 + 64 + 16 + 8 + 8 + 8 + 8;

        public String32 Nickname => EndianUnsafe.FromBytes<String32>(Bytes, 0); // 32B
        public String64 Password => EndianUnsafe.FromBytes<String64>(Bytes, 32); // 64B
        public byte[] KeyAES => new Span<byte>(Bytes, 96, 16).ToArray(); // 16B
        public long ServerSecret => EndianUnsafe.FromBytes<long>(Bytes, 112); // 8B
        public long ChallengeID => EndianUnsafe.FromBytes<long>(Bytes, 120); // 8B
        public long Timestamp => EndianUnsafe.FromBytes<long>(Bytes, 128); // 8B
        public long RunID => EndianUnsafe.FromBytes<long>(Bytes, 136); // 8B

        public AllowConnection() { }
        public AllowConnection(string nickname, string password, byte[] keyAES, long serverSecret, long challengeID, long timestamp, long runID, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes<String32>(nickname),
                EndianUnsafe.GetBytes<String64>(password),
                keyAES?.Length == 16 ? keyAES : throw new ArgumentException("KeyAES must have length of exactly 16 bytes."),
                EndianUnsafe.GetBytes(serverSecret),
                EndianUnsafe.GetBytes(challengeID),
                EndianUnsafe.GetBytes(timestamp),
                EndianUnsafe.GetBytes(runID)
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes?.Length == SIZE &&
                Validation.IsGoodNickname(Nickname) &&
                Validation.IsGoodPassword(Password);
        }
    }
}
