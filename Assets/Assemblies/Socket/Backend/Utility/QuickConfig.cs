#nullable enable

namespace Larnix.Socket.Backend.Utility;

public record QuickConfig(
    ushort Port,
    ushort MaxConnections,
    bool IsLoopback,
    ISecretRepository SecretRepository,
    IUserRepository UserRepository,
    int IdentityMaskIPv4 = 32,
    int IdentityMaskIPv6 = 56,
    string? RelayAddress = null
    );
