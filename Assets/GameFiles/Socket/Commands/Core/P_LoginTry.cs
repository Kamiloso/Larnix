using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Linq;
using Larnix.Socket.Channel;

namespace Larnix.Socket.Commands
{
    public class P_LoginTry : BaseCommand
    {
        public const int SIZE = 32 + 64 + 8 + 8 + 8 + 64;

        public string Nickname { get; private set; } // 32B (16 chars)
        public string Password { get; private set; } // 64B (32 chars)
        public long ServerSecret { get; private set; } // 8B
        public long ChallengeID { get; private set; } // 8B
        public long Timestamp { get; private set; } // 8B

        // -- Additional Feature ---
        public string NewPassword { get; private set; } // 64B (32 chars)

        public P_LoginTry(string nickname, string password, long serverSecret, long challengeID, long timestamp, byte code = 0)
            : base(code)
        {
            Nickname = nickname;
            Password = password;
            ServerSecret = serverSecret;
            ChallengeID = challengeID;
            Timestamp = timestamp;
            NewPassword = password;

            DetectDataProblems();
        }

        public void SetNewPassword(string newPassword)
        {
            NewPassword = newPassword;

            DetectDataProblems();
        }

        public P_LoginTry(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length != SIZE) {
                HasProblems = true;
                return;
            }

            Nickname = Common.FixedBinaryToString(bytes[0..32]);
            Password = Common.FixedBinaryToString(bytes[32..96]);
            ServerSecret = EndianUnsafe.FromBytes<long>(bytes, 96);
            ChallengeID = EndianUnsafe.FromBytes<long>(bytes, 104);
            Timestamp = EndianUnsafe.FromBytes<long>(bytes, 112);
            NewPassword = Common.FixedBinaryToString(bytes[120..184]);

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                Common.StringToFixedBinary(Nickname, 16),
                Common.StringToFixedBinary(Password, 32),
                EndianUnsafe.GetBytes(ServerSecret),
                EndianUnsafe.GetBytes(ChallengeID),
                EndianUnsafe.GetBytes(Timestamp),
                Common.StringToFixedBinary(NewPassword, 32)
                );
            return new Packet(ID, Code, bytes);
        }
        protected override void DetectDataProblems()
        {
            bool ok = (
                Common.IsGoodNickname(Nickname) &&
                Common.IsGoodPassword(Password) &&
                Common.IsGoodPassword(NewPassword)
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
