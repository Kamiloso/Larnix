#nullable enable
using Larnix.Socket.Payload;

namespace Larnix.Socket.Session.Components;

internal class HeaderProvider
{
    private readonly Seqs _seqs;

    public HeaderProvider(Seqs seqs)
    {
        _seqs = seqs;
    }

    public PayloadHeader NextSyn()
    {
        return NextSafe()
            .WithFlag(PacketFlag.SYN)
            .WithFlag(PacketFlag.RSA);
    }

    public PayloadHeader NextSafe()
    {
        return new PayloadHeader(
            seqNum: ++_seqs.SeqNum,
            ackNum: _seqs.RcvNum,
            flags: 0x00
            );
    }

    public PayloadHeader NextFast()
    {
        return new PayloadHeader(
            seqNum: _seqs.SeqNum,
            ackNum: _seqs.RcvNum,
            flags: (byte)PacketFlag.FAS
            );
    }

    public PayloadHeader NextFin()
    {
        return NextFast()
            .WithFlag(PacketFlag.FIN);
    }
}
