using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Linq;

namespace Larnix.Socket.Commands
{
    public class AllowConnection : BaseCommand
    {
        public override Name ID => Name.AllowConnection;
        public const int SIZE = 32 + 64 + 16;

        public string Nickname { get; private set; } // 32B (16 chars)
        public string Password { get; private set; } // 64B (32 chars)
        public byte[] KeyAES { get; private set; } // 16B

        public AllowConnection(string nickname, string password, byte[] keyAES, byte code = 0)
            : base(Name.None, code)
        {
            Nickname = nickname;
            Password = password;
            KeyAES = keyAES;

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

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes1 = Common.StringToFixedBinary(Nickname, 16);
            byte[] bytes2 = Common.StringToFixedBinary(Password, 32);
            byte[] bytes3 = KeyAES;

            byte[] bytes = bytes1.Concat(bytes2).Concat(bytes3).ToArray();

            return new Packet((byte)ID, Code, bytes);
        }
        protected override void DetectDataProblems()
        {
            bool ok = (
                Common.IsGoodNickname(Nickname) &&
                Common.IsGoodPassword(Password)
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
