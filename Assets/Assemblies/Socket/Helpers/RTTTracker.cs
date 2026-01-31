using System;
using System.Collections.Generic;
using Larnix.Core.Utils;

namespace Larnix.Socket.Helpers
{
    internal class RTTTracker
    {
        private const int MAX_RTTS = 10;
        private readonly float FALLBACK_RTT, OFFSET_RTT;

        private readonly LinkedList<(int seq, long time)> PacketTimestamps = new();
        private readonly LinkedList<long> PacketRTTs = new();
        private bool _rttRecalculate = true;
        private float _rttCache;

        public RTTTracker(long fallbackRTT, long offsetRTT)
        {
            FALLBACK_RTT = Miliseconds(fallbackRTT);
            OFFSET_RTT = Miliseconds(offsetRTT);
        }

        public void Ping(int seq)
        {
            PacketTimestamps.ForEachRemove(tuple => !Timestamp.InTimestamp(tuple.time));
            PacketTimestamps.AddLast((seq, Timestamp.GetTimestamp()));
        }

        public void Pong(int seq)
        {
            PacketTimestamps.ForEachRemove(tuple => tuple.seq - seq <= 0, tuple =>
            {
                long delta = Timestamp.GetTimestamp() - tuple.time;

                PacketRTTs.AddLast(delta);
                if (PacketRTTs.Count > MAX_RTTS)
                {
                    PacketRTTs.RemoveFirst();
                }
            });
        }

        public void ForgetSequences(HashSet<int> sequences)
        {
            if (sequences.Count > 0)
                PacketTimestamps.ForEachRemove(tuple => sequences.Contains(tuple.seq));
        }

        public long WaitingTimeMs() =>
            (long)Math.Round((AvgRTT + OFFSET_RTT) * 1000f);

        public float AvgRTT
        {
            get
            {
                if (_rttRecalculate)
                {
                    if (PacketRTTs.Count == 0)
                        _rttCache = FALLBACK_RTT; // not enough data
                    else
                        _rttCache = (float)PacketRTTs.Median() / 1000.0f;
                }

                return _rttCache;
            }
        }

        private static float Miliseconds(long ms) => ms * 0.001f;
    }
}
