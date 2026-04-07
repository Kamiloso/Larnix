using Larnix.Core;

namespace Larnix.Socket.Packets;

internal class HeaderSpan
{
    public byte[] AllBytes { get; private set; }

    public CmdID ID => Binary<CmdID>.Deserialize(AllBytes, 0);
    public byte Code => Binary<byte>.Deserialize(AllBytes, 4);

    public HeaderSpan(byte[] packetBytes)
    {
        AllBytes = packetBytes;
    }
}
