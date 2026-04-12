#nullable enable
using System.Collections.Generic;

namespace Larnix.Core.Limiters;

public interface ILimiterOf<T>
{
    bool TryAdd(T key);
    void Remove(T key);
}

public class LimiterOf<T> : ILimiterOf<T>
{
    public ulong Max { get; }

    private readonly Dictionary<T, Limiter> _limiters = new();

    public LimiterOf(ulong max)
    {
        Max = max;
    }

    public ulong Current(T key)
    {
        return _limiters.TryGetValue(key, out var limiter) ? limiter.Current : 0;
    }

    public bool TryAdd(T key)
    {
        if (!_limiters.TryGetValue(key, out var limiter))
        {
            limiter = new Limiter(Max);
            _limiters[key] = limiter;
        }

        return limiter.TryAdd();
    }

    public void Remove(T key)
    {
        if (!_limiters.TryGetValue(key, out var limiter)) return;

        limiter.Remove();

        if (limiter.Current == 0)
        {
            _limiters.Remove(key);
        }
    }

    public void Reset()
    {
        _limiters.Clear();
    }
}
