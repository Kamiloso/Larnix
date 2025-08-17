using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Linq;

namespace Larnix.Socket.Commands
{
    public class A_ServerInfo : BaseCommand
    {
        public override Name ID => Name.A_ServerInfo;
        public const int SIZE = 256 + 8 + 512 + 2 + 2 + 4 + 8;

        public byte[] PublicKeyModulus {  get; private set; } // 256B
        public byte[] PublicKeyExponent { get; private set; } // 8B
        public string Motd { get; private set; } // 512B (256 chars)
        public ushort CurrentPlayers { get; private set; } // 2B
        public ushort MaxPlayers { get; private set; } // 2B
        public uint GameVersion { get; private set; } // 4B
        public long PasswordIndex { get; private set; } // 8B

        public A_ServerInfo(
            byte[] publicKeyModulus,
            byte[] publicKeyExponent,
            string motd,
            ushort currentPlayers,
            ushort maxPlayers,
            uint gameVersion,
            long passwordIndex,
            byte code = 0
            )
            : base(Name.None, code)
        {
            PublicKeyExponent = publicKeyExponent;
            PublicKeyModulus = publicKeyModulus;
            Motd = motd;
            CurrentPlayers = currentPlayers;
            MaxPlayers = maxPlayers;
            GameVersion = gameVersion;
            PasswordIndex = passwordIndex;

            DetectDataProblems();
        }

        public A_ServerInfo(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length < SIZE) { // can be bigger than size for future compatibility
                HasProblems = true;
                return;
            }

            PublicKeyModulus = bytes[0..256];
            PublicKeyExponent = bytes[256..264];
            Motd = Common.FixedBinaryToString(bytes[264..776]);
            CurrentPlayers = EndianUnsafe.FromBytes<ushort>(bytes, 776);
            MaxPlayers = EndianUnsafe.FromBytes<ushort>(bytes, 778);
            GameVersion = EndianUnsafe.FromBytes<uint>(bytes, 780);
            PasswordIndex = EndianUnsafe.FromBytes<long>(bytes, 784);

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                PublicKeyModulus,
                PublicKeyExponent,
                Common.StringToFixedBinary(Motd, 256),
                EndianUnsafe.GetBytes(CurrentPlayers),
                EndianUnsafe.GetBytes(MaxPlayers),
                EndianUnsafe.GetBytes(GameVersion),
                EndianUnsafe.GetBytes(PasswordIndex)
                );

            return new Packet((byte)ID, Code, bytes);
        }
        protected override void DetectDataProblems()
        {
            bool ok = (
                PublicKeyModulus.Length == 256 &&
                PublicKeyExponent.Length == 8 &&
                Common.IsGoodMessage(Motd)
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
