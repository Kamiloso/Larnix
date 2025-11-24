using System.Collections;
using System.Collections.Generic;
using Larnix.Socket.Channel;

namespace Larnix.Socket.Packets
{
    public class Stop : Payload
    {
        private const int SIZE = 0;

        public Stop() { }
        public Stop(byte code)
        {
            InitializePayload(new byte[0], code);
        }

        protected override bool IsValid()
        {
            return Bytes?.Length == SIZE;
        }
    }
}
