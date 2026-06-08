#nullable enable
using Larnix.Socket.Payload.Structs;
using System.Net;
using Larnix.Core;
using Larnix.Socket.Server.Configuration;
using Larnix.Socket.Server.Channel;

namespace Larnix.Socket.Server;

internal class InfoProvider
{
    public ushort PlayerCount => _clients.Count;
    public ushort MaxPlayers => _settings.MaxPlayers;

    private readonly QuickSettings _settings;
    private readonly QuickSecurity _security;
    private readonly SecretProvider _secrets;
    private readonly ClientSessions _clients;

    public InfoProvider(MainServices services, ClientSessions clients)
    {
        _settings = services.Settings;
        _security = services.Security;
        _secrets = services.Secrets;
        _clients = clients;
    }

    public ServerInfo CreateServerInfo()
    {
        return new ServerInfo(
            mayRegister: _settings.EnableRegister,
            players: PlayerCount,
            maxPlayers: MaxPlayers,
            gameVersion: _settings.Version,
            timestamp: Timestamp.Now(),
            runId: _secrets.RunId,
            motd: _settings.Motd,
            hostUser: _settings.HostUser,
            rsaPublicKey: FixedRsaPublic.FromKey(_secrets.Rsa)
            );
    }

    public string GetCIDR(IPEndPoint target)
    {
        return WebIdentity.GetCIDR(
            address: target.Address,
            subnet: target.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                ? _security.IdentityMaskIPv4
                : _security.IdentityMaskIPv6
            );
    }
}
