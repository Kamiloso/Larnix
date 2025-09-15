using System.Collections;
using System.Collections.Generic;
using System;

namespace QuickNet.Channel.Cmds
{
    public class A_ServerInfo : Payload
    {
        private const int SIZE = 264 + 2 + 2 + 4 + 8 + 8 + 8 + 256 + 256 + 256;
        private const int CH_START = 272;

        public byte[] PublicKey => new Span<byte>(Bytes, 0, 264).ToArray(); // 264B
        public ushort CurrentPlayers => EndianUnsafe.FromBytes<ushort>(Bytes, 264); // 2B
        public ushort MaxPlayers => EndianUnsafe.FromBytes<ushort>(Bytes, 266); // 2B
        public uint GameVersion => EndianUnsafe.FromBytes<uint>(Bytes, 268); // 4B
        public long ChallengeID => GetChallengeID_ThreadSafe(); // 8B
        public long Timestamp => EndianUnsafe.FromBytes<long>(Bytes, 280); // 8B
        public long RunID => EndianUnsafe.FromBytes<long>(Bytes, 288); // 8B
        public String256 UserText1 => EndianUnsafe.FromBytes<String256>(Bytes, 296); // 256B (128 chars)
        public String256 UserText2 => EndianUnsafe.FromBytes<String256>(Bytes, 552); // 256B (128 chars)
        public String256 UserText3 => EndianUnsafe.FromBytes<String256>(Bytes, 808); // 256B (128 chars)

        public A_ServerInfo() { }
        public A_ServerInfo(byte[] publicKey, ushort currentPlayers, ushort maxPlayers, uint gameVersion, long challengeID, long timestamp, long runID,
            string userText1 = "", string userText2 = "", string userText3 = "", byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                publicKey?.Length == 264 ? publicKey : throw new ArgumentException("PublicKey must have length of exactly 264 bytes."),
                EndianUnsafe.GetBytes(currentPlayers),
                EndianUnsafe.GetBytes(maxPlayers),
                EndianUnsafe.GetBytes(gameVersion),
                EndianUnsafe.GetBytes(challengeID),
                EndianUnsafe.GetBytes(timestamp),
                EndianUnsafe.GetBytes(runID),
                EndianUnsafe.GetBytes<String256>(userText1),
                EndianUnsafe.GetBytes<String256>(userText2),
                EndianUnsafe.GetBytes<String256>(userText3)
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes?.Length == SIZE &&
                Validation.IsGoodText<String256>(UserText1) &&
                Validation.IsGoodText<String256>(UserText2) &&
                Validation.IsGoodText<String256>(UserText3);
        }

        private static object locker = new();
        internal void IncrementChallengeID_ThreadSafe(long delta)
        {
            lock (locker)
            {
                long value = ChallengeID;
                value += delta;
                byte[] bytes = EndianUnsafe.GetBytes(value);
                Buffer.BlockCopy(bytes, 0, Bytes, CH_START, bytes.Length);
            }
        }
        private long GetChallengeID_ThreadSafe()
        {
            lock (locker)
            {
                return EndianUnsafe.FromBytes<long>(Bytes, CH_START);
            }
        }
    }
}
