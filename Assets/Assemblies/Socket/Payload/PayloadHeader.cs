#nullable enable
using System;
using System.Runtime.InteropServices;
using Larnix.Socket.Payload.Structs;

namespace Larnix.Socket.Payload;

[Flags]
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
    private const short PROTOCOL_VERSION = 5;

    public readonly short ProtocolVersion;
    public readonly Seq SeqNum;
    public readonly Seq AckNum;
    public readonly byte Flags;

    public PayloadHeader(short protocolVersion, Seq seqNum, Seq ackNum, byte flags)
    {
        ProtocolVersion = protocolVersion;
        SeqNum = seqNum;
        AckNum = ackNum;
        Flags = flags;
    }

    public PayloadHeader(Seq seqNum, Seq ackNum, byte flags)
    {
        this = new PayloadHeader(
            PROTOCOL_VERSION,
            seqNum, ackNum, flags
            );
    }

    public PayloadHeader(Seq seqNum, byte flags)
    {
        this = new PayloadHeader(
            PROTOCOL_VERSION,
            seqNum, new Seq(0), flags
            );
    }

    public bool CompatibleProtocolVersion() => ProtocolVersion == PROTOCOL_VERSION;

    public bool HasFlag(PacketFlag flag) => (Flags & (byte)flag) != 0;
    public PayloadHeader WithFlag(PacketFlag flag) => new(SeqNum, AckNum, (byte)(Flags | (byte)flag));
    public PayloadHeader WithoutFlag(PacketFlag flag) => new(SeqNum, AckNum, (byte)(Flags & (byte)~flag));
}
