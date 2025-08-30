using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using QuickNet.Channel;
using QuickNet.Data;

namespace QuickNet.Commands
{
    public class A_ServerInfo : BaseCommand
    {
        public const int SIZE = 264 + 2 + 2 + 4 + 8 + 8 + 3 * 256;

        public byte[] PublicKey;        // 264B (256 + 8)
        public ushort CurrentPlayers;   // 2B
        public ushort MaxPlayers;       // 2B
        public uint GameVersion;        // 4B
        public long ChallengeID;        // 8B
        public long Timestamp;          // 8B
        public string UserText1;        // 256B (128 chars)
        public string UserText2;        // 256B (128 chars)
        public string UserText3;        // 256B (128 chars)

        public A_ServerInfo(
            byte[] publicKey,
            ushort currentPlayers,
            ushort maxPlayers,
            uint gameVersion,
            long challengeID,
            long timestamp,

            string userText1 = "",
            string userText2 = "",
            string userText3 = "",

            byte code = 0
            )
            : base(code)
        {
            PublicKey = publicKey;
            CurrentPlayers = currentPlayers;
            MaxPlayers = maxPlayers;
            GameVersion = gameVersion;
            ChallengeID = challengeID;
            Timestamp = timestamp;
            UserText1 = userText1;
            UserText2 = userText2;
            UserText3 = userText3;

            DetectDataProblems();
        }

        public A_ServerInfo(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length != SIZE) {
                HasProblems = true;
                return;
            }

            PublicKey = bytes[0..264];
            CurrentPlayers = EndianUnsafe.FromBytes<ushort>(bytes, 264);
            MaxPlayers = EndianUnsafe.FromBytes<ushort>(bytes, 266);
            GameVersion = EndianUnsafe.FromBytes<uint>(bytes, 268);
            ChallengeID = EndianUnsafe.FromBytes<long>(bytes, 272);
            Timestamp = EndianUnsafe.FromBytes<long>(bytes, 280);
            UserText1 = StringUtils.FixedBinaryToString(bytes.AsSpan(288, 256));
            UserText2 = StringUtils.FixedBinaryToString(bytes.AsSpan(544, 256));
            UserText3 = StringUtils.FixedBinaryToString(bytes.AsSpan(800, 256));

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                PublicKey,
                EndianUnsafe.GetBytes(CurrentPlayers),
                EndianUnsafe.GetBytes(MaxPlayers),
                EndianUnsafe.GetBytes(GameVersion),
                EndianUnsafe.GetBytes(ChallengeID),
                EndianUnsafe.GetBytes(Timestamp),
                StringUtils.StringToFixedBinary(UserText1, 128),
                StringUtils.StringToFixedBinary(UserText2, 128),
                StringUtils.StringToFixedBinary(UserText3, 128)
                );

            return new Packet(ID, Code, bytes);
        }
        protected override void DetectDataProblems()
        {
            bool ok = (
                PublicKey.Length == 264 &&
                Validation.IsGoodUserText(UserText1) &&
                Validation.IsGoodUserText(UserText2) &&
                Validation.IsGoodUserText(UserText3)
                );
            HasProblems = HasProblems || !ok;
        }

        private object locker = new();
        internal void IncrementChallengeID_ThreadSafe(long n)
        {
            lock (locker)
            {
                ChallengeID += n;
            }
        }
    }
}
