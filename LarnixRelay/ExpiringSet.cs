using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Larnix.Relay
{
    public class ExpiringSet<T>
    {
        private readonly Dictionary<T, long> _timestamps = new Dictionary<T, long>();
        private readonly object _lock = new object();
        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        private readonly long _ttlTicks;
        private readonly long _cleanupIntervalTicks;
        private long _lastCleanup;

        public ExpiringSet(TimeSpan ttl, TimeSpan cleanupInterval)
        {
            _ttlTicks = ttl.Ticks;
            _cleanupIntervalTicks = cleanupInterval.Ticks;
            _lastCleanup = _stopwatch.ElapsedTicks;
        }

        // returns true if element was ADDED (not refreshed)
        public bool Add(T item)
        {
            lock (_lock)
            {
                CleanupIfNeeded();
                var now = _stopwatch.ElapsedTicks;
                var existed = _timestamps.ContainsKey(item);
                _timestamps[item] = now; // add or refresh timestamp
                return !existed;
            }
        }

        public bool Contains(T item)
        {
            lock (_lock)
            {
                CleanupIfNeeded();
                return _timestamps.ContainsKey(item);
            }
        }

        public bool Remove(T item)
        {
            lock (_lock)
            {
                CleanupIfNeeded();
                return _timestamps.Remove(item);
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    CleanupIfNeeded();
                    return _timestamps.Count;
                }
            }
        }

        private void CleanupIfNeeded()
        {
            var now = _stopwatch.ElapsedTicks;
            if (now - _lastCleanup < _cleanupIntervalTicks)
                return;

            _lastCleanup = now;
            var expired = new List<T>();
            foreach (var kvp in _timestamps)
                if (now - kvp.Value > _ttlTicks)
                    expired.Add(kvp.Key);

            foreach (var key in expired)
                _timestamps.Remove(key);
        }
    }
}
