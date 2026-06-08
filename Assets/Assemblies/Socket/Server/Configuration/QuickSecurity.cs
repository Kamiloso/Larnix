#nullable enable
using static Larnix.Socket.Server.Configuration.QuickSecurity;

namespace Larnix.Socket.Server.Configuration;

public record QuickSecurity(
    int IdentityMaskIPv4,
    int IdentityMaskIPv6,
    LimiterStruct LightPackets, // any packet
    LimiterStruct HeavyPackets, // NCN / SYN / RSA packets (require entry processing)
    LimiterStruct Registers, // account creation
    ConcurrentLimiterStruct ConcurrentRsaDecryptions, // max concurrent decryptions
    ConcurrentLimiterStruct ConcurrentSessions, // max connections
    ConcurrentLimiterStruct ConcurrentHashings // max concurrent hashings
    )
{
    public record LimiterStruct(
        long Global,
        long PerNetwork,
        long ResetPeriodMs
        );

    public record ConcurrentLimiterStruct(
        long Global,
        long PerNetwork
        );

    public static QuickSecurity Default => new(
        IdentityMaskIPv4: 32,
        IdentityMaskIPv6: 56,
        LightPackets: new LimiterStruct(
            Global: long.MaxValue,
            PerNetwork: 1000,
            ResetPeriodMs: 1000 // 1 second
            ),
        HeavyPackets: new LimiterStruct(
            Global: 50,
            PerNetwork: 6,
            ResetPeriodMs: 3000 // 3 seconds
            ),
        Registers: new LimiterStruct(
            Global: 50,
            PerNetwork: 6,
            ResetPeriodMs: 3_600_000 // 1 hour
            ),
        ConcurrentRsaDecryptions: new ConcurrentLimiterStruct(
            Global: 12,
            PerNetwork: 2
            ),
        ConcurrentSessions: new ConcurrentLimiterStruct(
            Global: 100,
            PerNetwork: 5
            ),
        ConcurrentHashings: new ConcurrentLimiterStruct(
            Global: 12,
            PerNetwork: 2
            )
        );
}
