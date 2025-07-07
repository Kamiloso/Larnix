using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Linq;

namespace Larnix.Socket.Commands
{
    public class P_PasswordChange : BaseCommand
    {
        public override Name ID => Name.P_PasswordChange;
        public const int SIZE = 32 + 64 + 64;

        public string Nickname { get; private set; } // 32B (16 chars)
        public string OldPassword { get; private set; } // 64B (32 chars)
        public string NewPassword { get; private set; } // 64B (32 chars)

        public P_PasswordChange(string nickname, string oldPassword, string newPassword, byte code = 0)
            : base(Name.None, code)
        {
            Nickname = nickname;
            OldPassword = oldPassword;
            NewPassword = newPassword;

            DetectDataProblems();
        }

        public P_PasswordChange(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length != SIZE) {
                HasProblems = true;
                return;
            }

            Nickname = Common.FixedBinaryToString(bytes[0..32]);
            OldPassword = Common.FixedBinaryToString(bytes[32..96]);
            NewPassword = Common.FixedBinaryToString(bytes[96..160]);

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes1 = Common.StringToFixedBinary(Nickname, 16);
            byte[] bytes2 = Common.StringToFixedBinary(OldPassword, 32);
            byte[] bytes3 = Common.StringToFixedBinary(NewPassword, 32);

            byte[] bytes = bytes1.Concat(bytes2).Concat(bytes3).ToArray();
            return new Packet((byte)ID, Code, bytes);
        }
        protected override void DetectDataProblems()
        {
            bool ok = (
                Common.IsGoodNickname(Nickname) &&
                Common.IsGoodPassword(OldPassword) &&
                Common.IsGoodPassword(NewPassword)
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
