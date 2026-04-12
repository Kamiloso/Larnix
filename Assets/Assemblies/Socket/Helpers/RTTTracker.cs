#nullable enable
using System;
using System.Collections.Generic;
using Larnix.Model.Utils;

namespace Larnix.Socket.Helpers;

internal class RTTTracker
{
    private const int MAX_RTTS = 10;

    private readonly LinkedList<(int seq, long time)> _packetTimestamps = new();
    private readonly LinkedList<long> _packetRTTs = new();

    private float _rttCache;

    public long FallbackRTT { get; init; }
    public long OffsetRTT { get; init; }

    public float AvgRTT
    {
        get
        {
            if (_packetRTTs.Count == 0)
                _rttCache = FallbackRTT; // not enough data
            else
                _rttCache = (float)_packetRTTs.Median() / 1000.0f;

            return _rttCache;
        }
    }

    public void Ping(int seq)
    {
        _packetTimestamps.ForEachRemove(tuple => !Timestamp.InTimestamp(tuple.time));
        _packetTimestamps.AddLast((seq, Timestamp.GetTimestamp()));
    }

    public void Pong(int seq)
    {
        _packetTimestamps.ForEachRemove(tuple => tuple.seq - seq <= 0, tuple =>
        {
            long delta = Timestamp.GetTimestamp() - tuple.time;

            _packetRTTs.AddLast(delta);
            if (_packetRTTs.Count > MAX_RTTS)
            {
                _packetRTTs.RemoveFirst();
            }
        });
    }

    public void ForgetSequences(HashSet<int> sequences)
    {
        if (sequences.Count > 0)
            _packetTimestamps.ForEachRemove(tuple => sequences.Contains(tuple.seq));
    }

    public long WaitingTimeMs()
    {
        return (long)Math.Round((AvgRTT + OffsetRTT) * 1000f);
    }
}
