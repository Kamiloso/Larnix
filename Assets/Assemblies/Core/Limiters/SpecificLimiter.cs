#nullable enable

namespace Larnix.Core.Limiters;

public class SpecificLimiter<T> : ILimiter
{
    private readonly ILimiterOf<T> _limiter;
    private readonly T _key;

    public SpecificLimiter(ILimiterOf<T> limiter, T key)
    {
        _limiter = limiter;
        _key = key;
    }

    public bool TryAdd() => _limiter.TryAdd(_key);
    public void Remove() => _limiter.Remove(_key);
}
