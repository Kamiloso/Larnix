#nullable enable
using Larnix.Core;
using Larnix.Socket.Networking;
using Larnix.Socket.Payload;
using System;
using System.Threading.Tasks;
using Larnix.Socket.Client.Services;
using Larnix.Socket.Server.Users;
using Larnix.Socket.Server.Channel;
using Larnix.Socket.Server.Configuration;

namespace Larnix.Socket.Server;

public class QuickServer : ITickable, IDisposable
{
    public ushort Port => _triple.LocalPort;
    public ushort PlayerCount => _infoProvider.PlayerCount;
    public ushort MaxPlayers => _infoProvider.MaxPlayers;
    public string Authcode => _secrets.Authcode;
    public string? RelayAddress => _triple.RelayForeignAddressTask.Result;

    private readonly MainServices _services;
    private readonly TripleSocket _triple;
    private readonly Coroutines _coroutines;
    private readonly SecretProvider _secrets;
    private readonly Limiters _limiters;
    private readonly ClientSessions _clients;
    private readonly AsyncLogins _asyncLogins;
    private readonly InfoProvider _infoProvider;
    private readonly WebReceiver _webReceiver;

    public static async Task<QuickServer> CreateServerAsync(
        QuickSettings settings, QuickInterfaces interfaces, QuickSecurity? security = null)
    {
        security ??= QuickSecurity.Default;

        QuickServer server = new(settings, interfaces, security,
            out Task<string?> relayTask);

        await relayTask;
        return server;
    }

    private QuickServer(QuickSettings settings, QuickInterfaces interfaces, QuickSecurity security,
        out Task<string?> relayTask)
    {
        var secrets = interfaces.SecretRepository;

        _services = new MainServices(
            Socket: _triple = new TripleSocket(
                settings.Port,
                settings.IsLoopback,
                settings.RelayAddress
                ),
            Settings: settings,
            Interfaces: interfaces,
            Security: security,
            Coroutines: _coroutines = new Coroutines(),
            Secrets: _secrets = new SecretProvider(secrets),
            Limiters: _limiters = new Limiters(security)
            );

        _clients = new ClientSessions();
        _asyncLogins = new AsyncLogins(_services);
        _infoProvider = new InfoProvider(_services, _clients);

        _webReceiver = new WebReceiver(
            _services,
            _infoProvider,
            new AsyncDecryptor(_services),
            new ConnReceiver(_services, _clients, _asyncLogins),
            new RequestReceiver(_services, _infoProvider, _asyncLogins)
            );

        relayTask = _triple.RelayForeignAddressTask;
    }

    // ---------- SENDING ----------

    public void Send<T>(string nickname, in T payload) where T : unmanaged
        => _clients.Send(nickname, payload, true);

    public void SendUnreliable<T>(string nickname, in T payload) where T : unmanaged
        => _clients.Send(nickname, payload, false);

    public void Broadcast<T>(in T payload) where T : unmanaged
        => _clients.Broadcast(payload, true);

    public void BroadcastUnreliable<T>(in T payload) where T : unmanaged
        => _clients.Broadcast(payload, false);

    // ---------- RECEIVING ----------

    public void OnConnected(ConnectedHandler? execute)
        => _clients.OnConnected(execute);

    public void OnDisconnected(DisconnectedHandler? execute)
        => _clients.OnDisconnected(execute);

    public void OnReceive<T>(CmdSenderHandler<T>? execute) where T : unmanaged
        => _clients.OnReceive(execute);

    // ---------- OTHER ----------

    public void KickRequest(string nickname, Action? onKick = null)
        => _clients.KickRequest(nickname, onKick);

    public void ResetLimiters()
        => throw new NotImplementedException("TODO: implement");

    public void Tick(float deltaTime)
    {
        _limiters.Tick(deltaTime);
        _coroutines.Tick(deltaTime);
        _webReceiver.Tick(deltaTime);
        _clients.Tick(deltaTime);
    }

    public void Dispose()
    {
        // stop server -> clean local cache about it
        LocalCache.RemoveWhere(discovery => discovery.Authcode == Authcode);

        // dispose resources
        _webReceiver.Dispose();
        _coroutines.Dispose();
        _triple.Dispose();
    }
}
