using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Larnix.Socket.Channel;

namespace Larnix.Socket.Commands
{
    public class AllowConnection : BaseCommand
    {
        public const int SIZE = 32 + 64 + 16 + 8 + 8 + 8;

        public string Nickname { get; private set; } // 32B (16 chars)
        public string Password { get; private set; } // 64B (32 chars)
        public byte[] KeyAES { get; private set; } // 16B
        public long ServerSecret { get; private set; } // 8B
        public long ChallengeID { get; private set; } // 8B
        public long Timestamp { get; private set; } // 8B

        public AllowConnection(string nickname, string password, byte[] keyAES, long serverSecret, long challengeID, long timestamp, byte code = 0)
            : base(code)
        {
            Nickname = nickname;
            Password = password;
            KeyAES = keyAES;
            ServerSecret = serverSecret;
            ChallengeID = challengeID;
            Timestamp = timestamp;

            DetectDataProblems();
        }

        public AllowConnection(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length != SIZE) {
                HasProblems = true;
                return;
            }

            Nickname = Common.FixedBinaryToString(bytes[0..32]);
            Password = Common.FixedBinaryToString(bytes[32..96]);
            KeyAES = bytes[96..112];
            ServerSecret = EndianUnsafe.FromBytes<long>(bytes, 112);
            ChallengeID = EndianUnsafe.FromBytes<long>(bytes, 120);
            Timestamp = EndianUnsafe.FromBytes<long>(bytes, 128);

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                Common.StringToFixedBinary(Nickname, 16),
                Common.StringToFixedBinary(Password, 32),
                KeyAES,
                EndianUnsafe.GetBytes(ServerSecret),
                EndianUnsafe.GetBytes(ChallengeID),
                EndianUnsafe.GetBytes(Timestamp)
            );

            return new Packet(ID, Code, bytes);
        }

        protected override void DetectDataProblems()
        {
            bool ok = (
                Common.IsGoodNickname(Nickname) &&
                Common.IsGoodPassword(Password) &&
                KeyAES.Length == 16
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
