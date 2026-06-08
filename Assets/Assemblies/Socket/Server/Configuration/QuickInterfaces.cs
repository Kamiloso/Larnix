#nullable enable
using Larnix.Socket.Server.Interfaces;

namespace Larnix.Socket.Server.Configuration;

public record QuickInterfaces(
    IBanProvider BanProvider,
    IPasswordHasher PasswordHasher,
    ISecretRepository SecretRepository,
    IQuickUserRepository UserRepository
    );
