using Larnix.Core;
using Larnix.Socket.Structs;
using System.Collections;
using System.Collections.Generic;

namespace Larnix.Socket
{
    internal class HeaderSpan
    {
        public byte[] AllBytes { get; private set; }

        public CmdID ID => EndianUnsafe.FromBytes<CmdID>(AllBytes, 0);
        public byte Code => EndianUnsafe.FromBytes<byte>(AllBytes, 4);

        public HeaderSpan(byte[] packetBytes)
        {
            AllBytes = packetBytes;
        }
    }
}
