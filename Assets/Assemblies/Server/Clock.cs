#nullable enable
using System;
using Larnix.Core;

namespace Larnix.Server;

internal interface IClock : ITickable
{
    long ServerTick { get; }
    uint FixedFrame { get; }
    float DeltaTime { get; }
    float TPS { get; }
}

internal class Clock : IClock
{
    public long ServerTick { get; private set; }
    public uint FixedFrame { get; private set; }

    private float? _deltaTime = null;
    private InvalidOperationException NoDeltaTimeException => new("Delta time uninitialized.");

    public float DeltaTime => _deltaTime != null ?
        _deltaTime.Value : throw NoDeltaTimeException;
    public float TPS => _deltaTime != null ?
        1f / _deltaTime.Value : throw NoDeltaTimeException;

    public Clock(long serverTick)
    {
        ServerTick = serverTick;
        FixedFrame = 1;
    }

    public void Tick(float deltaTime)
    {
        _deltaTime = deltaTime;
        FixedFrame++;
        ServerTick++;
    }
}
