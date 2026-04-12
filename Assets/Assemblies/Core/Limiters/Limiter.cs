#nullable enable
using System;

namespace Larnix.Core.Limiters;

public interface ILimiter
{
    bool TryAdd();
    void Remove();
}

public class Limiter : ILimiter
{
    public ulong Max { get; }
    public ulong Current { get; private set; }

    public Limiter(ulong max)
    {
        Max = max;
        Current = 0;
    }

    public bool TryAdd()
    {
        if (Current < Max)
        {
            Current++;
            return true;
        }
        return false;
    }

    public void Remove()
    {
        Current = Math.Max(0, Current - 1);
    }

    public void Reset()
    {
        Current = 0;
    }
}
