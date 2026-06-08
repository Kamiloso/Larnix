#nullable enable
using Larnix.Core;
using Larnix.Socket.Limiting;
using Larnix.Socket.Server.Configuration;

namespace Larnix.Socket.Server;

internal class Limiters : ITickable
{
    public TrafficLimiter<string> LightPackets { get; }
    public TrafficLimiter<string> HeavyPackets { get; }
    public TrafficLimiter<string> Registers { get; }
    public TrafficLimiter<string> ConcurrentRsaDecryptions { get; }
    public TrafficLimiter<string> ConcurrentSessions { get; }
    public TrafficLimiter<string> ConcurrentHashings { get; }

    private readonly CycleTimer _lightPacketsLimiter;
    private readonly CycleTimer _heavyPacketsLimiter;
    private readonly CycleTimer _registersLimiter;

    public Limiters(QuickSecurity security)
    {
        LightPackets = new TrafficLimiter<string>(
            maxTrafficLocal: security.LightPackets.PerNetwork,
            maxTrafficGlobal: security.LightPackets.Global
            );

        HeavyPackets = new TrafficLimiter<string>(
            maxTrafficLocal: security.HeavyPackets.PerNetwork,
            maxTrafficGlobal: security.HeavyPackets.Global
            );

        Registers = new TrafficLimiter<string>( // TODO: bind limiter
            maxTrafficLocal: security.Registers.PerNetwork,
            maxTrafficGlobal: security.Registers.Global
            );

        ConcurrentRsaDecryptions = new TrafficLimiter<string>(
            maxTrafficLocal: security.ConcurrentRsaDecryptions.PerNetwork,
            maxTrafficGlobal: security.ConcurrentRsaDecryptions.Global
            );

        ConcurrentSessions = new TrafficLimiter<string>(
            maxTrafficLocal: security.ConcurrentSessions.PerNetwork,
            maxTrafficGlobal: security.ConcurrentSessions.Global
            );

        ConcurrentHashings = new TrafficLimiter<string>( // TODO: bind limiter
            maxTrafficLocal: security.ConcurrentHashings.PerNetwork,
            maxTrafficGlobal: security.ConcurrentHashings.Global
            );

        _lightPacketsLimiter = new CycleTimer(security.LightPackets.ResetPeriodMs);
        _lightPacketsLimiter.OnInterval += LightPackets.Reset;

        _heavyPacketsLimiter = new CycleTimer(security.HeavyPackets.ResetPeriodMs);
        _heavyPacketsLimiter.OnInterval += HeavyPackets.Reset;

        _registersLimiter = new CycleTimer(security.Registers.ResetPeriodMs);
        _registersLimiter.OnInterval += Registers.Reset;
    }

    public void Tick(float deltaTime)
    {
        _lightPacketsLimiter.Tick(deltaTime);
        _heavyPacketsLimiter.Tick(deltaTime);
        _registersLimiter.Tick(deltaTime);
    }
}
