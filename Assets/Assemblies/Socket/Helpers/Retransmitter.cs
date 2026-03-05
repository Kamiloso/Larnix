using Larnix.Socket.Packets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Larnix.Socket.Helpers
{
    internal class Retransmitter
    {
        private readonly int RETRY_COUNT;
        
        private readonly Dictionary<PayloadBox, int> _triesLeft = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<PayloadBox, long> _retryAt = new(ReferenceEqualityComparer.Instance);

        public Retransmitter(int retryCount)
        {
            RETRY_COUNT = retryCount;
        }

        public void Add(PayloadBox box, long miliseconds)
        {
            _triesLeft[box] = RETRY_COUNT;
            _retryAt[box] = Timestamp.GetTimestamp() + miliseconds;
        }

        public void Discard(PayloadBox box)
        {
            _triesLeft.Remove(box);
            _retryAt.Remove(box);
        }

        public bool AllowRetransmission(PayloadBox box, long miliseconds, out bool isTimeout)
        {
            isTimeout = false;

            if (!_triesLeft.ContainsKey(box))
                throw new InvalidOperationException("Non-existing PayloadBox!");

            if (_triesLeft[box] <= 0)
            {
                isTimeout = true;
                return false;
            }

            if (Timestamp.GetTimestamp() > _retryAt[box])
            {
                _triesLeft[box]--;
                _retryAt[box] = Timestamp.GetTimestamp() + miliseconds;

                return true;
            }

            return false;
        }

        private class ReferenceEqualityComparer : IEqualityComparer<PayloadBox>
        {
            public static readonly ReferenceEqualityComparer Instance = new();

            public bool Equals(PayloadBox x, PayloadBox y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(PayloadBox obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
