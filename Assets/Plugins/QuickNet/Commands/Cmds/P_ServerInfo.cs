using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using QuickNet.Channel;
using QuickNet.Data;

namespace QuickNet.Commands
{
    public class P_ServerInfo : BaseCommand
    {
        public const int SIZE = 32;

        public string Nickname { get; private set; } // 32B (16 chars)

        public P_ServerInfo(string nickname, byte code = 0)
            : base(code)
        {
            Nickname = nickname;

            DetectDataProblems();
        }

        public P_ServerInfo(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length != SIZE) {
                HasProblems = true;
                return;
            }

            Nickname = StringUtils.FixedBinaryToString(bytes.AsSpan(0, 32));

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = StringUtils.StringToFixedBinary(Nickname, 16);
            return new Packet(ID, Code, bytes);
        }
        protected override void DetectDataProblems()
        {
            bool ok = (
                Validation.IsGoodNickname(Nickname)
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
