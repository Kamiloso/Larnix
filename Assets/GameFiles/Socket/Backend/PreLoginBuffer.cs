using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System.Threading.Tasks;
using Larnix.Socket.Commands;

namespace Larnix.Socket.Backend
{
    public class PreLoginBuffer
    {
        public readonly AllowConnection AllowConnection;

        public readonly EndPoint EndPoint;
        private readonly List<byte[]> Buffer = new List<byte[]>(MaxPackets);
        public const int MaxPackets = 32;

        public PreLoginBuffer(AllowConnection allowConnection)
        {
            AllowConnection = allowConnection;
        }

        public void AddPacket(byte[] bytes)
        {
            if(Buffer.Count < MaxPackets)
                Buffer.Add(bytes);
        }

        public List<byte[]> GetBuffer()
        {
            return Buffer;
        }
    }
}
