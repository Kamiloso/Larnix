using System.Collections;
using System.Collections.Generic;

namespace QuickNet.Channel.Cmds
{
    public class None : Payload
    {
        private const int SIZE = 0;

        public None() { }
        public None(byte code)
        {
            InitializePayload(new byte[0], code);
        }

        protected override bool IsValid()
        {
            return Bytes?.Length == SIZE;
        }
    }
}
