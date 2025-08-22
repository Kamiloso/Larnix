using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Linq;

namespace Larnix.Socket.Commands
{
    public class A_LoginTry : BaseCommand
    {
        public override Name ID => Name.A_LoginTry;
        public const int SIZE = 0;

        // code 0 -> false
        // code 1 -> true

        public A_LoginTry(byte code = 0)
            : base(Name.None, code)
        {
            DetectDataProblems();
        }

        public A_LoginTry(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length != SIZE) {
                HasProblems = true;
                return;
            }

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            return new Packet((byte)ID, Code, new byte[0]);
        }
        protected override void DetectDataProblems()
        {
            bool ok = (
                Code == 0 || Code == 1
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
