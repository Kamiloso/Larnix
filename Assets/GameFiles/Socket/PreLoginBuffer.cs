using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System.Threading.Tasks;
using Larnix.Socket.Commands;

namespace Larnix.Socket
{
    public class PreLoginBuffer
    {
        public readonly AllowConnection AllowConnection;

        public readonly EndPoint EndPoint;
        private readonly List<byte[]> Buffer = new List<byte[]>();
        public const uint MAX_PACKETS = 1024;

        public PreLoginBuffer(AllowConnection allowConnection)
        {
            AllowConnection = allowConnection;
        }

        public void AddPacket(byte[] bytes)
        {
            if(Buffer.Count < MAX_PACKETS)
                Buffer.Add(bytes);
        }

        public List<byte[]> GetBuffer()
        {
            return Buffer;
        }
    }
}
