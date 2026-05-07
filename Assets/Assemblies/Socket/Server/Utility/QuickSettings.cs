#nullable enable
using Larnix.Core;
using Larnix.Core.Serialization;
using Larnix.Socket.Server.Interfaces;

namespace Larnix.Socket.Server.Utility;

public record QuickSettings
{
    public ushort Port { get; }
    public ushort MaxPlayers { get; }
    public bool IsLoopback { get; }
    public bool EnableRegister { get; }
    public FixedString256 Motd { get; }
    public FixedString32 HostUser { get; }
    public Version Version { get; }
    public InterfacesStruct Interfaces { get; }
    public SecurityStruct Security { get; }
    public string? RelayAddress { get; }

    public QuickSettings(
        ushort port,
        ushort maxPlayers,
        bool isLoopback,
        bool enableRegister,
        FixedString256 motd,
        FixedString32 hostUser,
        Version version,
        InterfacesStruct interfaces,
        SecurityStruct? security = default,
        string? relayAddress = null
        )
    {
        Port = port;
        MaxPlayers = maxPlayers;
        IsLoopback = isLoopback;
        EnableRegister = enableRegister;
        Motd = motd;
        HostUser = hostUser;
        Version = version;
        Interfaces = interfaces;
        Security = security ?? SecurityStruct.Default;
        RelayAddress = relayAddress;
    }

    public record InterfacesStruct(
        ISecretRepository SecretRepository,
        IQuickUserRepository UserRepository,
        IPasswordHasher PasswordHasher,
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
                Registers: new LimiterStruct( // TODO: implement register limiter
                    Global: 50,
                    PerNetwork: 6,
                    ResetPeriodMs: 3_600_000 // 1 hour
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
