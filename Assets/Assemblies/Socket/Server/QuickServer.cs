#nullable enable
using Larnix.Core;
using Larnix.Socket.Server.Utility;
using Larnix.Socket.Networking;
using Larnix.Socket.Payload;
using Larnix.Socket.Security.Keys;
using System;
using System.Threading.Tasks;
using Larnix.Socket.Server.Receivers;
using Larnix.Socket.Client;

namespace Larnix.Socket.Server;

public class QuickServer : ITickable, IDisposable
{
    public ushort Port => _triple.LocalPort;
    public ushort PlayerCount => _clients.Count;
    public ushort MaxPlayers => _settings.MaxPlayers;
    public string Authcode => _infoProvider.Authcode;
    public string? RelayAddress => _triple.RelayForeignAddressTask.Result;

    private readonly TripleSocket _triple;
    private readonly KeyRsa _rsa;
    private readonly Coroutines _coroutines;
    private readonly Clients _clients;
    private readonly InfoProvider _infoProvider;
    private readonly AsyncLogins _asyncLogins;
    private readonly WebReceiver _webReceiver;
    private readonly QuickSettings _settings;

    public static async Task<QuickServer> CreateServerAsync(QuickSettings settings)
    {
        QuickServer server = new(settings, out Task<string?> relayTask);
        await relayTask;
        return server;
    }

    private QuickServer(QuickSettings settings, out Task<string?> relayTask)
    {
        _triple = new TripleSocket(
            settings.Port,
            settings.IsLoopback,
            settings.RelayAddress
            );

        _rsa = KeyRsa.FromSecretRepo(
            settings.Interfaces.SecretRepository,
            SocketInfo.PathPrivateKey
            );

        _coroutines = new Coroutines();
        _clients = new Clients();
        _infoProvider = new InfoProvider(settings, _rsa, _clients);
        _asyncLogins = new AsyncLogins(_infoProvider, settings);
        _webReceiver = new WebReceiver(_triple, _rsa, _clients, _asyncLogins, _infoProvider, _coroutines, settings);

        _settings = settings;

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
        _coroutines.Tick(deltaTime);
        _webReceiver.Tick(deltaTime);
        _clients.Tick(deltaTime);
    }

    public void Dispose()
    {
        // stop server -> clean local cache about it
        string authcode = _infoProvider.Authcode;
        LocalCache.RemoveWhere(discovery => discovery.Authcode == authcode);

        // dispose resources
        _webReceiver.Dispose();
        _coroutines.Dispose();
        _rsa.Dispose();
        _triple.Dispose();
    }
}
