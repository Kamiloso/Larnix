using Larnix.Packets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Larnix.Socket.Channel
{
    internal class Retransmitter
    {
        public int RetryCount { get; private set; }

        private readonly Dictionary<PayloadBox, int> _triesLeft = new(); // reference comparation
        private readonly Dictionary<PayloadBox, long> _retryAt = new(); // reference comparation

        public Retransmitter(int retryCount)
        {
            RetryCount = retryCount;
        }

        public void Add(PayloadBox box, long miliseconds)
        {
            _triesLeft[box] = RetryCount;
            _retryAt[box] = Timestamp.GetTimestamp() + miliseconds;
        }

        public void Discard(PayloadBox box)
        {
            _triesLeft.Remove(box);
            _retryAt.Remove(box);
        }

        public bool AllowRetransmission(PayloadBox box, long miliseconds)
        {
            if (!_triesLeft.ContainsKey(box))
                throw new InvalidOperationException("Non-existing PayloadBox!");

            if (_triesLeft[box] <= 0) // timeout check and throw informative exception
                throw new TimeoutException();

            if (_retryAt[box] < Timestamp.GetTimestamp()) // allow condition
            {
                _triesLeft[box]--;
                _retryAt[box] = Timestamp.GetTimestamp() + miliseconds;

                return true;
            }

            return false;
        }
    }
}
