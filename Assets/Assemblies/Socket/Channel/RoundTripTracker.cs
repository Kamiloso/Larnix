#nullable enable
using Larnix.Core;
using System;
using System.Collections.Generic;

namespace Larnix.Socket.Channel;

internal class RoundTripTracker
{
    private static int RttCacheSize => 10;
    private static long DefaultRtt => 200; // ms

    private readonly Dictionary<PendingPacket, long> _starts = new();

    private readonly long[] _rtts = new long[RttCacheSize];
    private int _rttIndex = 0;

    private long Now => Timestamp.Now();

    public RoundTripTracker()
    {
        for (int i = 0; i < RttCacheSize; i++)
        {
            _rtts[i] = DefaultRtt;
        }
    }

    public void StartMeasure(PendingPacket packet)
    {
        _starts.Add(packet, Now);
    }

    public void StopMeasure(PendingPacket packet)
    {
        _starts.Remove(packet, out long before);
        _rtts[_rttIndex] = Now - before;
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

        if (span.Length % 2 == 1)
        {
            return span[span.Length / 2];
        }
        else
        {
            return (span[span.Length / 2 - 1] + span[span.Length / 2]) / 2;
        }
    }
}
