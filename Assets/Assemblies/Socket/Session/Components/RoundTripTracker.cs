#nullable enable
using Larnix.Core;
using System;
using System.Collections.Generic;

namespace Larnix.Socket.Session.Components;

internal class RoundTripTracker
{
    private static int RttCacheSize => 10;
    private static long DefaultRtt => 200; // ms

    private readonly Dictionary<PendingPacket, long> _starts = new();

    private readonly long[] _rtts = new long[RttCacheSize];
    private int _rttIndex = 0;

    public RoundTripTracker()
    {
        for (int i = 0; i < RttCacheSize; i++)
        {
            _rtts[i] = DefaultRtt;
        }
    }

    public void StartMeasure(PendingPacket packet)
    {
        _starts.Add(packet, Timestamp.Now());
    }

    public void StopMeasure(PendingPacket packet)
    {
        _starts.Remove(packet, out long before);
        _rtts[_rttIndex] = Timestamp.Now() - before;
        _rttIndex = (_rttIndex + 1) % RttCacheSize;
    }

    public void TerminateMeasure(PendingPacket packet)
    {
        _starts.Remove(packet);
    }

    public long MedianRtt()
    {
        Span<long> span = stackalloc long[RttCacheSize]; // copy to stack
        for (int i = 0; i < RttCacheSize; i++)
        {
            span[i] = _rtts[i];
        }

        for (int i = 1; i < span.Length; i++) // insertion sort
        {
            long key = span[i];
            int j = i - 1;
            while (j >= 0 && span[j] > key)
            {
                span[j + 1] = span[j];
                j--;
            }
            span[j + 1] = key;
        }

        return span.Length % 2 == 0
            ? (span[span.Length / 2 - 1] + span[span.Length / 2]) / 2
            : span[span.Length / 2];
    }
}
