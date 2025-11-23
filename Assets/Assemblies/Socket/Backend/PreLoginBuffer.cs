using System.Collections;
using System.Collections.Generic;
using System.Net;
using Socket.Channel.Cmds;

namespace Socket.Backend
{
    internal class PreLoginBuffer
    {
        internal readonly AllowConnection AllowConnection;

        internal readonly EndPoint EndPoint;
        private readonly List<byte[]> Buffer = new List<byte[]>(MaxPackets);
        internal const int MaxPackets = 32;

        internal PreLoginBuffer(AllowConnection allowConnection)
        {
            AllowConnection = allowConnection;
        }

        internal void AddPacket(byte[] bytes)
        {
            if(Buffer.Count < MaxPackets)
                Buffer.Add(bytes);
        }

        internal List<byte[]> GetBuffer()
        {
            return Buffer;
        }
    }
}
