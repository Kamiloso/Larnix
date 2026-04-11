#nullable enable

namespace Larnix.Model;

public static class GameInfo
{
    public static Version Version => new("0.0.48.1");

    public static ushort DefaultPort => 27682;
    public static ushort DefaultRelayPort => 27681;
    public static string DefaultRelayAddress => "relay.se3.page";

    public static string ReservedNickname => "Player";
    public static string ReservedPassword => "SGP_PASSWORD\x01";
    public static string DefaultMotd => "Welcome to the server!";

    public static int TargetTPS => 50;
    public static float FixedTime => 1f / TargetTPS;
}
