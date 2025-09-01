using System;
using System.Collections;
using System.Collections.Generic;
using QuickNet.Channel;
using QuickNet.Channel.Cmds;

namespace Larnix.Packets
{
    public class CodeInfo : Payload
    {
        private const int SIZE = 0;

        public new Info Code => (Info)base.Code;

        public enum Info : byte
        {
            YouDie,
            RespawnMe,
        }

        public CodeInfo() { }
        public CodeInfo(Info code)
        {
            InitializePayload(new byte[0], (byte)code);
        }

        protected override bool IsValid()
        {
            return Bytes?.Length == SIZE;
        }
    }
}
