#nullable enable
using Larnix.Core;
using Larnix.Socket.Backend.Utility;
using Larnix.Socket.Networking;
using Larnix.Socket.Payload;
using Larnix.Socket.Security.Keys;
using System;
using System.Threading.Tasks;

namespace Larnix.Socket.Backend;

public class QuickServer : ITickable, IDisposable
{
    public string? RelayAddress => _triple.RelayForeignAddressTask.Result;

    private readonly TripleSocket _triple;
    private readonly KeyRSA _rsa;
    private readonly Clients _clients;

    public static async Task<QuickServer> CreateServerAsync(QuickConfig settings)
    {
        QuickServer server = new(settings, out Task<string?> relayTask);
        await relayTask;
        return server;
    }

    private QuickServer(QuickConfig settings, out Task<string?> relayTask)
    {
        _triple = new TripleSocket(
            settings.Port,
            settings.IsLoopback,
            settings.RelayAddress
            );

        _rsa = KeyRSA.FromSecretRepo(settings.SecretRepository, "key-rsa");
        _clients = new Clients(settings);

        relayTask = _triple.RelayForeignAddressTask;
    }

    public void Send<T>(string nickname, in T payload) where T : unmanaged
        => _clients.Send(nickname, payload, true);

    public void SendUnreliable<T>(string nickname, in T payload) where T : unmanaged
        => _clients.Send(nickname, payload, false);

    public void OnReceive<T>(CmdSenderHandler<T>? execute) where T : unmanaged
        => _clients.OnReceive(execute);

    public void Tick(float deltaTime)
    {
        _clients.Tick(deltaTime);
    }

    public void Dispose()
    {
        _clients.Dispose();
        _rsa.Dispose();
        _triple.Dispose();
    }
}
