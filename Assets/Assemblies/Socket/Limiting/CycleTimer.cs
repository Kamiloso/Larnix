#nullable enable
using Larnix.Core;
using System;

namespace Larnix.Socket.Limiting;

public class CycleTimer : ITickable
{
    public long Interval { get; }
    public long Accumulator { get; private set; }

    public event Action? OnInterval;

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
            OnInterval?.Invoke();
            Accumulator %= Interval;
        }
    }
}
