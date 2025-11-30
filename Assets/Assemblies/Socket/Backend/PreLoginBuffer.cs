using System.Collections;
using System.Collections.Generic;
using System.Net;
using Larnix.Socket.Packets;

namespace Larnix.Socket.Backend
{
    internal class PreLoginBuffer
    {
        public readonly AllowConnection AllowConnection;
        public readonly EndPoint EndPoint;

        private readonly Queue<byte[]> Buffer = new(MaxPackets);
        private const int MaxPackets = 64;

        public PreLoginBuffer(AllowConnection allowConnection)
        {
            AllowConnection = allowConnection;
        }

        public void Push(byte[] bytes)
        {
            if(Buffer.Count < MaxPackets)
                Buffer.Enqueue(bytes);
        }

        public byte[] Pop()
        {
            if (Buffer.Count > 0)
                return Buffer.Dequeue();
            return null;
        }
    }
}
