using Larnix.Core;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Binary;

namespace Larnix.Socket.Packets
{
    internal class HeaderSpan
    {
        public byte[] AllBytes { get; private set; }

        public CmdID ID => Primitives.FromBytes<CmdID>(AllBytes, 0);
        public byte Code => Primitives.FromBytes<byte>(AllBytes, 4);

        public HeaderSpan(byte[] packetBytes)
        {
            AllBytes = packetBytes;
        }
    }
}
