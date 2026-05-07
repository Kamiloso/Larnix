#nullable enable
using Larnix.Core;
using Larnix.Core.Serialization;
using Larnix.Core.Utils;
using Larnix.Socket.Helpers;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Security.Keys;
using Larnix.Socket.Server.Utility;
using System.Net;

namespace Larnix.Socket.Server;

internal interface IInfoProvider
{
    string Authcode { get; }

    ServerInfo CreateServerInfo();
    Credentials CreateCredentials(in FixedString32 nickname, in FixedString64 password, long challengeId);

    bool CheckGlobalCredentials(in Credentials credentials);
    string GetCIDR(IPEndPoint target);
}

internal class InfoProvider : IInfoProvider
{
    public string Authcode { get; }

    private readonly QuickSettings _settings;
    private readonly KeyRsa _rsa;
    private readonly IClients _clients;

    private readonly long _runId;
    private readonly long _serverSecret;

    public InfoProvider(QuickSettings settings, KeyRsa rsa, IClients clients)
    {
        _settings = settings;
        _rsa = rsa;
        _clients = clients;

        _runId = RandUtils.SecureLong();

        string? readSecret = _settings.Interfaces.SecretRepository.ReadSecret(SocketInfo.PathServerSecret);
        if (readSecret == null || !long.TryParse(readSecret, out _serverSecret))
        {
            _serverSecret = RandUtils.SecureLong();
            _settings.Interfaces.SecretRepository.StoreSecret(
                SocketInfo.PathServerSecret,
                _serverSecret.ToString()
                );
        }

        Authcode = Security.Authcode.ProduceAuthCodeRSA(
            key: _rsa.ExportPublicKey().Bytes264(),
            secret: _serverSecret
            );
    }

    public ServerInfo CreateServerInfo()
    {
        return new ServerInfo(
            mayRegister: _settings.EnableRegister,
            players: _clients.Count,
            maxPlayers: _settings.MaxPlayers,
            gameVersion: _settings.Version,
            timestamp: Timestamp.Now(),
            runId: _runId,
            motd: _settings.Motd,
            hostUser: _settings.HostUser,
            rsaPublicKey: _rsa.ExportPublicKey()
            );
    }

    public Credentials CreateCredentials(in FixedString32 nickname, in FixedString64 password, long challengeId)
    {
        return new Credentials(
            nickname: nickname,
            password: password,
            serverSecret: _serverSecret,
            runId: _runId,
            timestamp: Timestamp.Now(),
            challengeId: challengeId
            );
    }

    public bool CheckGlobalCredentials(in Credentials credentials)
    {
        return credentials.ServerSecret == _serverSecret
            && credentials.RunId == _runId
            && Timestamp.IsWithin(credentials.Timestamp);
    }

    public string GetCIDR(IPEndPoint target)
    {
        return WebIdentity.GetCIDR(
            address: target.Address,
            subnet: target.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                ? _settings.Security.IdentityMaskIPv4
                : _settings.Security.IdentityMaskIPv6
            );
    }
}
