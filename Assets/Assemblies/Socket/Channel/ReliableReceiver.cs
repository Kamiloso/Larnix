#nullable enable
using System.Collections.Generic;
using Larnix.Socket.Payload;
using Larnix.Socket.Payload.Structs;

namespace Larnix.Socket.Channel;

internal class ReliableReceiver
{
    private int WindowSize => 128;

    private readonly Queue<byte[]> _fastBuffer = new();
    private readonly Dictionary<Seq, byte[]> _buffer = new();
    private readonly Seqs _seqs;

    private Seq _nextSeq = new(1);

    public ReliableReceiver(Seqs seqs)
    {
        _seqs = seqs;
    }

    public void Push(PayloadHeader header, byte[] data)
    {
        Seq seqLast = _seqs.RcvNum;
        Seq seqNew = header.SeqNum;

        if (header.HasFlag(PacketFlag.FAS))
        {
            if (seqNew < seqLast - WindowSize) return;
            if (seqNew > seqLast + WindowSize) return;

            _fastBuffer.Enqueue(data);
        }
        else
        {
            if (seqNew <= seqLast) return;
            if (seqNew > seqLast + WindowSize) return;

            _buffer[seqNew] = data;
        }

        while (_buffer.ContainsKey(_seqs.RcvNum + 1))
        {
            _seqs.RcvNum++;
        }
    }

    public bool TryPop(out byte[] data)
    {
        if (_fastBuffer.TryDequeue(out data))
        {
            return true;
        }

        if (_buffer.TryGetValue(_nextSeq, out data))
        {
            _buffer.Remove(_nextSeq++);
            return true;
        }

        data = default!;
        return false;
    }
}
