#nullable enable
using System.Runtime.InteropServices;
using Larnix.Socket.Payload.Structs;

namespace Larnix.Socket.Payload;

internal enum PacketFlag : byte
{
    SYN = 1 << 0, // start connection (client -> server)
    FIN = 1 << 1, // end connection
    FAS = 1 << 2, // fast message / raw acknowledgement
    RSA = 1 << 3, // encrypted with RSA
    NCN = 1 << 4, // no connection
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct PayloadHeader
{
    private const ushort PROTOCOL_VERSION = 5;

    private readonly ushort ProtocolVersion;
    public readonly Seq SeqNum;
    public readonly Seq AckNum;
    public readonly byte Flags;

    public PayloadHeader(Seq seqNum, Seq ackNum, byte flags)
    {
        ProtocolVersion = PROTOCOL_VERSION;
        SeqNum = seqNum;
        AckNum = ackNum;
        Flags = flags;
    }

    public bool HasFlag(PacketFlag flag) => (Flags & (byte)flag) != 0;
    public PayloadHeader WithFlag(PacketFlag flag) => new(SeqNum, AckNum, (byte)(Flags | (byte)flag));
    public PayloadHeader WithoutFlag(PacketFlag flag) => new(SeqNum, AckNum, (byte)(Flags & (byte)~flag));

    public bool CompatibleProtocolVersion() => ProtocolVersion == PROTOCOL_VERSION;
}
