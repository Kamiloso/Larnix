using Larnix.Core;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Binary;

namespace Larnix.Packets
{
    public class HeaderSpan
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
