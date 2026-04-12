#nullable enable

namespace Larnix.Core.Limiters;

public class GroupLimiter : ILimiter
{
    private readonly ILimiter[] _limiters;

    public GroupLimiter(params ILimiter[] limiters)
    {
        _limiters = limiters;
    }

    public bool TryAdd()
    {
        for (int i = 0; i < _limiters.Length; i++)
        {
            if (!_limiters[i].TryAdd())
            {
                for (i -= 1; i >= 0; i--) // rollback
                {
                    _limiters[i].Remove();
                }
                return false;
            }
        }
        return true;
    }

    public void Remove()
    {
        foreach (var limiter in _limiters)
        {
            limiter.Remove();
        }
    }
}
