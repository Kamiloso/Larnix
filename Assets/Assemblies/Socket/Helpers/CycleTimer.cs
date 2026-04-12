#nullable enable
using System;
using Larnix.Core;

namespace Larnix.Socket.Helpers;

internal class CycleTimer : ITickable
{
    public long Interval { get; }
    public long Accumulator { get; private set; }

    public event Action? OnTick;

    public CycleTimer(long interval)
    {
        Interval = interval;
        Accumulator = 0;
    }

    public void Tick(float deltaTime)
    {
        Accumulator += (long)(deltaTime * 1000f);
        if (Accumulator > Interval)
        {
            OnTick?.Invoke();
            Accumulator %= Interval;
        }
    }
}
