#nullable enable
using Larnix.Socket.Payload;

namespace Larnix.Socket.Channel;

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
        byte flags = 0;
        flags |= (byte)PacketFlag.FAS;

        return new PayloadHeader(
            seqNum: _seqs.SeqNum,
            ackNum: _seqs.RcvNum,
            flags: flags
            );
    }

    public PayloadHeader NextFin()
    {
        return NextFast()
            .WithFlag(PacketFlag.FIN);
    }
}
