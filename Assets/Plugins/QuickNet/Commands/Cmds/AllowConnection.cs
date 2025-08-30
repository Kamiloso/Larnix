using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using QuickNet.Channel;
using QuickNet.Data;

namespace QuickNet.Commands
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

            Nickname = StringUtils.FixedBinaryToString(bytes.AsSpan(0, 32));
            Password = StringUtils.FixedBinaryToString(bytes.AsSpan(32, 64));
            KeyAES = bytes[96..112];
            ServerSecret = EndianUnsafe.FromBytes<long>(bytes, 112);
            ChallengeID = EndianUnsafe.FromBytes<long>(bytes, 120);
            Timestamp = EndianUnsafe.FromBytes<long>(bytes, 128);

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                StringUtils.StringToFixedBinary(Nickname, 16),
                StringUtils.StringToFixedBinary(Password, 32),
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
                Validation.IsGoodNickname(Nickname) &&
                Validation.IsGoodPassword(Password) &&
                KeyAES.Length == 16
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
