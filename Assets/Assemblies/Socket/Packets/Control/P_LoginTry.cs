using System;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Core.Binary;

namespace Larnix.Socket.Packets.Control
{
    internal class P_LoginTry : Payload
    {
        private const int SIZE = 32 + 64 + 64 + 8 + 8 + 8 + 8;

        public String32 Nickname => EndianUnsafe.FromBytes<String32>(Bytes, 0); // 32B
        public String64 Password => EndianUnsafe.FromBytes<String64>(Bytes, 32); // 64B
        public String64 NewPassword => EndianUnsafe.FromBytes<String64>(Bytes, 96); // 64B
        public long ServerSecret => EndianUnsafe.FromBytes<long>(Bytes, 160); // 8B
        public long ChallengeID => EndianUnsafe.FromBytes<long>(Bytes, 168); // 8B
        public long Timestamp => EndianUnsafe.FromBytes<long>(Bytes, 176); // 8B
        public long RunID => EndianUnsafe.FromBytes<long>(Bytes, 184); // 8B

        public P_LoginTry() { }
        public P_LoginTry(string nickname, string password, long serverSecret, long challengeID, long timestamp, long runID, string newPassword = null, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes<String32>(nickname),
                EndianUnsafe.GetBytes<String64>(password),
                EndianUnsafe.GetBytes<String64>(newPassword ?? password),
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
                Validation.IsGoodPassword(Password) &&
                Validation.IsGoodPassword(NewPassword);
        }
    }
}
