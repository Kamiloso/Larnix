using System;

namespace Larnix.Socket.Helpers.Limiters
{
    public class TrafficLimiter<T>
    {
        public ulong MaxTrafficLocal => _localLimiter.Max;
        public ulong MaxTrafficGlobal => _globalLimiter.Max;
        
        private readonly Limiter<T> _localLimiter;
        private readonly Limiter _globalLimiter;

        public TrafficLimiter(ulong maxTrafficLocal, ulong maxTrafficGlobal)
        {
            _localLimiter = new Limiter<T>(maxTrafficLocal);
            _globalLimiter = new Limiter(maxTrafficGlobal);
        }

        public ulong CurrentLocal(T key) => _localLimiter.Current(key);
        public ulong CurrentGlobal() => _globalLimiter.Current;

        public bool TryAdd(T key)
        {
            MoreLimiters limiters = new(
                (() => _globalLimiter.TryAdd(), () => _globalLimiter.Remove()),
                (() => _localLimiter.TryAdd(key), () => _localLimiter.Remove(key))
            );
            return limiters.TryAdd();
        }

        public void Remove(T key)
        {
            _localLimiter.Remove(key);
            _globalLimiter.Remove();
        }

        public void Reset()
        {
            _localLimiter.Reset();
            _globalLimiter.Reset();
        }
    }
}