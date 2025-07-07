using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Linq;

namespace Larnix.Socket.Commands
{
    public class Stop : BaseCommand
    {
        public override Name ID => Name.Stop;

        public Stop(byte code = 0)
            : base(Name.None, code)
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
            return new Packet((byte)ID, Code, null);
        }
        protected override void DetectDataProblems()
        {
            HasProblems = HasProblems || false;
        }
    }
}
