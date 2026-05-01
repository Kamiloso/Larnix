#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Helpers;
using System.Net;

namespace Larnix.Socket.Server.Utility;

public record QuickConfig
{
    public ushort Port { get; }
    public ushort MaxPlayers { get; }
    public bool IsLoopback { get; }
    public bool EnableRegister { get; }
    public FixedString256 Motd { get; }
    public FixedString32 HostUser { get; }
    public InterfacesStruct Interfaces { get; }
    public SecurityStruct Security { get; }
    public string? RelayAddress { get; }

    public QuickConfig(
        ushort port,
        ushort maxPlayers,
        bool isLoopback,
        InterfacesStruct interfaces,
        SecurityStruct? security = default,
        string? relayAddress = null
        )
    {
        Port = port;
        MaxPlayers = maxPlayers;
        IsLoopback = isLoopback;
        Interfaces = interfaces;
        Security = security ?? SecurityStruct.Default;
        RelayAddress = relayAddress;
    }

    public record InterfacesStruct(
        ISecretRepository SecretRepository,
        IQuickUserRepository UserRepository,
        IBanProvider BanProvider
        );

    public record SecurityStruct(
        int IdentityMaskIPv4,
        int IdentityMaskIPv6,
        LimiterRepoStruct Limiters
        )
    {
        public static SecurityStruct Default => new(
            IdentityMaskIPv4: 32,
            IdentityMaskIPv6: 56,
            Limiters: new LimiterRepoStruct(
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
                Requests: new LimiterStruct(
                    Global: long.MaxValue,
                    PerNetwork: 10,
                    ResetPeriodMs: 60_000 // 1 minute
                    ),
                Decryptions: new ConcurrentLimiterStruct(
                    Global: 12,
                    PerNetwork: 2
                    ),
                Connections: new ConcurrentLimiterStruct(
                    Global: long.MaxValue,
                    PerNetwork: 5
                    )
                )
            );
    }

    public record LimiterRepoStruct(
        LimiterStruct HeavyPackets,
        LimiterStruct Registers,
        LimiterStruct Requests,
        ConcurrentLimiterStruct Decryptions,
        ConcurrentLimiterStruct Connections
        );

    public record LimiterStruct(
        long Global,
        long PerNetwork,
        long ResetPeriodMs
        );

    public record ConcurrentLimiterStruct(
        long Global,
        long PerNetwork
        );
}
