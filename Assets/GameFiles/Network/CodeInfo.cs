using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using QuickNet.Channel;
using QuickNet.Commands;

namespace Larnix.Network
{
    public class CodeInfo : BaseCommand
    {
        public enum Info : byte
        {
            YouDie,
            RespawnMe,
        }

        public CodeInfo(byte code = 0)
            : base(code)
        {
            DetectDataProblems();
        }

        public CodeInfo(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes != null && bytes.Length != 0) {
                HasProblems = true;
                return;
            }

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            return new Packet(ID, Code, null);
        }

        protected override void DetectDataProblems()
        {
            bool ok = (
                true
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
