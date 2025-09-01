using System.Collections;
using System.Collections.Generic;

namespace QuickNet.Channel.Cmds
{
    public class A_LoginTry : Payload
    {
        private const int SIZE = 0;

        public bool Success => Code == 1;

        public A_LoginTry() { }
        public A_LoginTry(bool success)
        {
            InitializePayload(new byte[0], (byte)(success ? 1 : 0));
        }

        protected override bool IsValid()
        {
            return Bytes?.Length == SIZE;
        }
    }
}
