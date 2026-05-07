#nullable enable
using System;

namespace Larnix.Socket.Limiters;

internal interface ILimiter
{
    bool TryAdd();
    void Remove();
}

internal class Limiter : ILimiter
{
    public long Max { get; }
    public long Current { get; private set; }

    public Limiter(long max)
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
