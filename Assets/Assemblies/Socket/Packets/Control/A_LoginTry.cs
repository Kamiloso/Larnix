using System.Collections;
using System.Collections.Generic;
using Larnix.Socket.Packets;
using System;

namespace Larnix.Socket.Packets.Control
{
    internal sealed class A_LoginTry : Payload
    {
        private const int SIZE = 0;

        public bool Success => Code == 1;

        public A_LoginTry() { }
        public A_LoginTry(bool success)
        {
            InitializePayload(Array.Empty<byte>(),
                code: (byte)(success ? 1 : 0));
        }

        protected override bool IsValid()
        {
            return Bytes.Length == SIZE;
        }
    }
}
