#nullable enable
using Larnix.Core;
using Larnix.Core.Serialization;

namespace Larnix.Socket.Server.Configuration;

public record QuickSettings(
    ushort Port,
    ushort MaxPlayers,
    bool IsLoopback,
    bool EnableRegister,
    FixedString256 Motd,
    FixedString32 HostUser,
    Version Version,
    string? RelayAddress = null
    );
