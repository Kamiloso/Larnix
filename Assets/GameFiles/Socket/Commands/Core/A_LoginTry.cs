using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Linq;
using Larnix.Socket.Channel;

namespace Larnix.Socket.Commands
{
    public class A_LoginTry : BaseCommand
    {
        public const int SIZE = 0;

        // code 0 -> false
        // code 1 -> true

        public A_LoginTry(byte code = 0)
            : base(code)
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
            return new Packet(ID, Code, new byte[0]);
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
