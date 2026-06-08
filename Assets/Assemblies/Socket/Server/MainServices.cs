#nullable enable
using Larnix.Core;
using Larnix.Socket.Networking;
using Larnix.Socket.Server.Configuration;

namespace Larnix.Socket.Server;

internal record MainServices(
    ISocket Socket,
    QuickSettings Settings,
    QuickInterfaces Interfaces,
    QuickSecurity Security,
    Coroutines Coroutines,
    SecretProvider Secrets,
    Limiters Limiters
    );
