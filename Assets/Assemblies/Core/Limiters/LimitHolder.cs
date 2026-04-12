#nullable enable
using System;

namespace Larnix.Core.Limiters;

public class LimitHolder : IDisposable
{
    private readonly ILimiter _limiter;
    private readonly bool _acquired;

    private bool _disposed;

    public LimitHolder(ILimiter limiter, out bool acquired)
    {
        _limiter = limiter;
        _acquired = _limiter.TryAdd();

        acquired = _acquired;
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
