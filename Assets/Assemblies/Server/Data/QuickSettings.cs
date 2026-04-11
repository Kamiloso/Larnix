#nullable enable
using Larnix.Core;
using Larnix.Model.Utils;
using System.IO;
using Larnix.Socket.Backend;
using System.Net;
using System.Linq;
using Larnix.Core.Serialization;
using Larnix.Model;

namespace Larnix.Server.Data;

internal class QuickSettings : IQuickConfig
{
    private IServer Server => GlobRef.Get<IServer>();
    private IWorldMetaManager WorldMetaManager => GlobRef.Get<IWorldMetaManager>();
    private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();

    // Dynamic settings
    public ushort MaxClients => ServerConfig.MaxPlayers;
    public FixedString256 Motd => new(ServerConfig.Motd);
    public FixedString32 HostUser => Server.ServerType == ServerType.Remote ?
        new FixedString32(GameInfo.ReservedNickname) :
        WorldMetaManager.HostNickname;

    // Static settings
    public ushort Port { get; init; }
    public bool IsLoopback { get; init; }
    public string DataPath { get; init; }
    public int MaskIPv4 { get; init; }
    public int MaskIPv6 { get; init; }
    public bool AllowRegistration { get; init; }

    public QuickSettings()
    {
        Port = Server.ServerType == ServerType.Remote ? ServerConfig.Port : (ushort)0;
        IsLoopback = Server.ServerType == ServerType.Local;
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
