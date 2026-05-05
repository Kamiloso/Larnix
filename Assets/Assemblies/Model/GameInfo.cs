#nullable enable
using Larnix.Core;
using Larnix.Socket;

namespace Larnix.Model;

public static class GameInfo
{
    public static Version Version => Version.FromString("0.0.48.1");

    public static ushort DefaultPort => SocketInfo.DefaultPort;
    public static ushort DefaultRelayPort => SocketInfo.DefaultRelayPort;
    public static string DefaultRelayAddress => "relay.se3.page";

    public static string ReservedNickname => SocketInfo.ReservedNickname;
    public static string ReservedPassword => SocketInfo.ReservedPassword;
    public static string DefaultMotd => SocketInfo.DefaultMotd;

    public static int TargetTPS => 50;
    public static float FixedTime => 1f / TargetTPS;
}
