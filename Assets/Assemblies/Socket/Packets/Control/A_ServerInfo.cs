using System.Collections;
using System.Collections.Generic;
using System;
using Larnix.Core.Binary;
using Larnix.Core.Utils;
using Larnix.Socket.Packets;
using Version = Larnix.Core.Version;

namespace Larnix.Socket.Packets.Control
{
    internal sealed class A_ServerInfo : Payload
    {
        private const int SIZE = 264 + 2 + 2 + 4 + 8 + 8 + 8 + 256 + 32;

        public byte[] PublicKey => new Span<byte>(Bytes, 0, 264).ToArray();
        public ushort CurrentPlayers => Primitives.FromBytes<ushort>(Bytes, 264);
        public ushort MaxPlayers => Primitives.FromBytes<ushort>(Bytes, 266);
        public Version GameVersion => new Version(Primitives.FromBytes<uint>(Bytes, 268));
        public long ChallengeID => Primitives.FromBytes<long>(Bytes, 272);
        public long Timestamp => Primitives.FromBytes<long>(Bytes, 280);
        public long RunID => Primitives.FromBytes<long>(Bytes, 288);
        public String256 Motd => Primitives.FromBytes<String256>(Bytes, 296);
        public String32 HostUser => Primitives.FromBytes<String32>(Bytes, 552);

        public A_ServerInfo() { }
        public A_ServerInfo(byte[] publicKey, ushort currentPlayers, ushort maxPlayers, Version gameVersion, long challengeID, long timestamp, long runID,
            string motd = "", string hostUser = "", byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                publicKey?.Length == 264 ? publicKey : throw new ArgumentException("PublicKey must have length of exactly 264 bytes."),
                Primitives.GetBytes(currentPlayers),
                Primitives.GetBytes(maxPlayers),
                Primitives.GetBytes(gameVersion.ID),
                Primitives.GetBytes(challengeID),
                Primitives.GetBytes(timestamp),
                Primitives.GetBytes(runID),
                Primitives.GetBytes<String256>(motd),
                Primitives.GetBytes<String32>(hostUser)
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes.Length == SIZE &&
                Validation.IsGoodText<String256>(Motd) &&
                Validation.IsGoodText<String32>(HostUser);
        }
    }
}