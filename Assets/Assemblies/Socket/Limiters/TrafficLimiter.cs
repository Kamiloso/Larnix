#nullable enable

namespace Larnix.Socket.Limiters;

internal class TrafficLimiter<T> : ILimiterOf<T>
{
    public long MaxLocal => _localLimiter.Max;
    public long MaxGlobal => _globalLimiter.Max;

    private readonly LimiterOf<T> _localLimiter;
    private readonly Limiter _globalLimiter;

    public TrafficLimiter(long maxTrafficLocal, long maxTrafficGlobal)
    {
        _localLimiter = new LimiterOf<T>(maxTrafficLocal);
        _globalLimiter = new Limiter(maxTrafficGlobal);
    }

    public long CurrentLocal(T key) => _localLimiter.Current(key);
    public long CurrentGlobal() => _globalLimiter.Current;

    public bool TryAdd(T key)
    {
        var global = _globalLimiter;
        var specific = new SpecificLimiter<T>(_localLimiter, key);

        GroupLimiter group = new(global, specific);
        return group.TryAdd();
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
