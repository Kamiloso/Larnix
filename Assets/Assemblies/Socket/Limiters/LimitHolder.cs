#nullable enable
using System;

namespace Larnix.Socket.Limiters;

internal class LimitHolder : IDisposable
{
    private readonly ILimiter _limiter;
    private readonly bool _acquired;

    private bool _disposed;

    private LimitHolder(ILimiter limiter, out bool acquired)
    {
        _limiter = limiter;
        _acquired = _limiter.TryAdd();

        acquired = _acquired;
    }

    public static LimitHolder Acquire(ILimiter limiter, out bool acquired)
    {
        return new LimitHolder(
            limiter,
            out acquired
            );
    }

    public static LimitHolder Acquire<T>(ILimiterOf<T> limiter, T key, out bool acquired)
    {
        return Acquire(new SpecificLimiter<T>(limiter, key), out acquired);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_acquired)
        {
            _limiter.Remove();
        }
    }
}
