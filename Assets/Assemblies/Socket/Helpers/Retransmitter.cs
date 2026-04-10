using Larnix.Socket.Packets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Larnix.Socket.Helpers;

internal class Retransmitter
{
    private readonly int RETRY_COUNT;

    private readonly Dictionary<PayloadBox_Legacy, int> _triesLeft = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<PayloadBox_Legacy, long> _retryAt = new(ReferenceEqualityComparer.Instance);

    public Retransmitter(int retryCount)
    {
        RETRY_COUNT = retryCount;
    }

    public void Add(PayloadBox_Legacy box, long miliseconds)
    {
        _triesLeft[box] = RETRY_COUNT;
        _retryAt[box] = Timestamp.GetTimestamp() + miliseconds;
    }

    public void Discard(PayloadBox_Legacy box)
    {
        _triesLeft.Remove(box);
        _retryAt.Remove(box);
    }

    public bool AllowRetransmission(PayloadBox_Legacy box, long miliseconds, out bool isTimeout)
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

    private class ReferenceEqualityComparer : IEqualityComparer<PayloadBox_Legacy>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public bool Equals(PayloadBox_Legacy x, PayloadBox_Legacy y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(PayloadBox_Legacy obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
