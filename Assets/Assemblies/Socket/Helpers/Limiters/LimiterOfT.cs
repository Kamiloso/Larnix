using System;
using System.Collections.Generic;

namespace Larnix.Socket.Helpers.Limiters
{
    public class Limiter<T>
    {
        public ulong Max { get; init; }
        private readonly Dictionary<T, Limiter> _limiters = new();
        private Limiter CreateLimiter() => new Limiter(Max);

        public Limiter(ulong max)
        {
            Max = max;
        }

        public ulong Current(T key)
        {
            if (!_limiters.TryGetValue(key, out var limiter))
                return 0;

            return limiter.Current;
        }

        public bool TryAdd(T key)
        {
            if (!_limiters.TryGetValue(key, out var limiter))
            {
                limiter = CreateLimiter();
                _limiters[key] = limiter;
            }
            
            return limiter.TryAdd();
        }

        public void Remove(T key)
        {
            if (!_limiters.TryGetValue(key, out var limiter))
                throw new KeyNotFoundException($"No limiter found for key: {key}");

            limiter.Remove();

            if (limiter.Current == 0)
                _limiters.Remove(key);
        }

        public void Reset()
        {
            _limiters.Clear();
        }
    }
}