#nullable enable

namespace Larnix.Socket.Limiting;

internal class LimitAdder : ILimiter
{
    private readonly ILimiter _limiter;

    public LimitAdder(ILimiter limiter)
    {
        _limiter = limiter;
    }

    public bool TryAdd() => _limiter.TryAdd();
    public void Remove() { }
}
