#nullable enable

namespace Larnix.Core.Limiters;

public class LimitAdder : ILimiter
{
    private readonly ILimiter _limiter;

    public LimitAdder(ILimiter limiter)
    {
        _limiter = limiter;
    }

    public bool TryAdd() => _limiter.TryAdd();
    public void Remove() { }
}
