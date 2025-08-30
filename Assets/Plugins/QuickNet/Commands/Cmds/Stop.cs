using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using QuickNet.Channel;

namespace QuickNet.Commands
{
    public class Stop : BaseCommand
    {
        public Stop(byte code = 0)
            : base(code)
        {
            DetectDataProblems();
        }

        public Stop(Packet packet)
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
            HasProblems = HasProblems || false;
        }
    }
}
