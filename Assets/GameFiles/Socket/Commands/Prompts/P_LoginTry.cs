using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Linq;

namespace Larnix.Socket.Commands
{
    public class P_LoginTry : BaseCommand
    {
        public override Name ID => Name.P_LoginTry;
        public const int SIZE = 32 + 64 + 8 + 8;

        public string Nickname { get; private set; } // 32B (16 chars)
        public string Password { get; private set; } // 64B (32 chars)
        public long ServerSecret { get; private set; } // 8B
        public long ChallengeID { get; private set; } // 8B

        public P_LoginTry(string nickname, string password, long serverSecret, long challengeID, byte code = 0)
            : base(Name.None, code)
        {
            Nickname = nickname;
            Password = password;
            ServerSecret = serverSecret;
            ChallengeID = challengeID;

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

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                Common.StringToFixedBinary(Nickname, 16),
                Common.StringToFixedBinary(Password, 32),
                EndianUnsafe.GetBytes(ServerSecret),
                EndianUnsafe.GetBytes(ChallengeID)
                );
            return new Packet((byte)ID, Code, bytes);
        }
        protected override void DetectDataProblems()
        {
            bool ok = (
                Common.IsGoodNickname(Nickname) &&
                Common.IsGoodPassword(Password) &&
                ChallengeID != 0 // it would be registration
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
