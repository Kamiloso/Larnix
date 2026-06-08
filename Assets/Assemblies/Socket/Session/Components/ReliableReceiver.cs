#nullable enable
using System.Collections.Generic;
using Larnix.Socket.Payload;
using Larnix.Socket.Payload.Structs;

namespace Larnix.Socket.Session.Components;

internal class ReliableReceiver
{
    private static int WindowSize => 128;

    private readonly Dictionary<Seq, byte[]> _buffer = new();
    private readonly Queue<byte[]> _fastBuffer = new();
    
    private readonly Seqs _seqs;
    private Seq _nextSeq;

    public ReliableReceiver(Seqs seqs)
    {
        _seqs = seqs;
        _nextSeq = _seqs.RcvNum + 1;
    }

    public void Push(PayloadHeader header, byte[] decrypted)
    {
        Seq seqLast = _seqs.RcvNum;
        Seq seqNew = header.SeqNum;

        if (header.HasFlag(PacketFlag.FAS))
        {
            if (seqNew < seqLast - WindowSize) return;
            if (seqNew > seqLast + WindowSize) return;

            _fastBuffer.Enqueue(decrypted);
        }
        else
        {
            if (seqNew <= seqLast) return;
            if (seqNew > seqLast + WindowSize) return;

            _buffer[seqNew] = decrypted;
        }

        while (_buffer.ContainsKey(_seqs.RcvNum + 1))
        {
            _seqs.RcvNum++;
        }
    }

    public bool TryPop(out byte[] decrypted)
    {
        if (_fastBuffer.TryDequeue(out decrypted))
        {
            return true;
        }

        if (_buffer.Remove(_nextSeq, out decrypted))
        {
            _nextSeq++;
            return true;
        }

        decrypted = default!;
        return false;
    }
}
