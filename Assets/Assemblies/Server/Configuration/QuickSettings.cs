using Larnix.Core;
using Larnix.GameCore.Utils;
using Larnix.Server.Data;
using System.IO;
using Larnix.Socket.Backend;
using System.Net;
using System.Linq;

namespace Larnix.Server.Configuration;

internal class QuickSettings : IQuickConfig
{
    private Server Server => GlobRef.Get<Server>();
    private DataSaver DataSaver => GlobRef.Get<DataSaver>();
    private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();

    // Dynamic settings
    public ushort MaxClients => ServerConfig.MaxPlayers;
    public String256 Motd => (String256)ServerConfig.Motd;
    public String32 HostUser => Server.Type == ServerType.Remote ?
        (String32)Common.ReservedNickname :
        DataSaver.HostNickname;

    // Static settings
    public bool IsLoopback { get; init; }
    public string DataPath { get; init; }
    public int MaskIPv4 { get; init; }
    public int MaskIPv6 { get; init; }
    public bool AllowRegistration { get; init; }

    public QuickSettings()
    {
        IsLoopback = Server.Type == ServerType.Local;
        DataPath = Path.Combine(Server.WorldPath, "Socket");

        MaskIPv4 = ServerConfig.Network_ClientIdentityPrefixSizeIPv4;
        MaskIPv6 = ServerConfig.Network_ClientIdentityPrefixSizeIPv6;
        AllowRegistration = ServerConfig.Network_AllowRegistration;
    }

    public bool IsBanned(string nickname)
    {
        return ServerConfig.Administration_Banned.Contains(nickname);
    }

    public bool IsBanned(IPAddress address)
    {
        return ServerConfig.Administration_Banned
            .Any(entry => Common.IsInNetworkString(address, entry));
    }
}
