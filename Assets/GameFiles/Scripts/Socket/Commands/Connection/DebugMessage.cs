using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Linq;

namespace Larnix.Socket.Commands
{
    public class DebugMessage : BaseCommand
    {
        public override Name ID => Name.DebugMessage;
        public const int SIZE = 512;

        public string Data { get; private set; } // 512B (256 chars)

        public DebugMessage(string data, byte code = 0)
            : base(Name.None, code)
        {
            Data = data;

            DetectDataProblems();
        }

        public DebugMessage(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length != SIZE) {
                HasProblems = true;
                return;
            }

            Data = Common.FixedBinaryToString(bytes[0..512]);

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = Common.StringToFixedBinary(Data, 256);
            return new Packet((byte)ID, Code, bytes);
        }
        protected override void DetectDataProblems()
        {
            bool ok = (
                Common.IsGoodMessage(Data)
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
