using System.Collections;
using System.Collections.Generic;
using Larnix.Packets;

namespace Larnix.Packets.Control
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
