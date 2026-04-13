#nullable enable
using Larnix.Core;
using Larnix.Model;
using Larnix.Socket.Channel;
using Larnix.Socket.Client.Records;
using Larnix.Socket.Networking;
using Larnix.Socket.Payload;
using Larnix.Socket.Payload.Packets;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Security.Keys;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Larnix.Socket.Client;

public class QuickClient : ITickable, IDisposable
{
    public float Ping => _conn.AvgRTT * 1000f; // ms
    public bool IsDead => _conn.IsDead;
    public IPEndPoint Target => _conn.Target;

    private readonly UdpClient2 _udp;
    private readonly KeyRSA _rsa;
    private readonly QuickConnection _conn;

    private event Action? _listeners;

    private bool _disposed;

    public static async Task<QuickClient?> CreateClientAsync(FullLoginData fullLogin)
    {
        var (address, _, _, _) = fullLogin;

        ServerDiscovery discovery = fullLogin.ToServerDiscovery();
        ServerLogin loginData = fullLogin.ToServerLogin();

        IPEndPoint? target = await DnsResolver.ResolveAsync(address, GameInfo.DefaultPort);
        if (target == null)
        {
            Echo.LogWarning("Couldn't resolve address: " + address);
            return null;
        }

        var recv = await Resolver.TryGetEntryTicketAsync(discovery);
        if (recv.Error != ResolveError.None)
        {
            Echo.LogWarning("Couldn't get entry ticket: " + recv.Error);
            return null;
        }

        EntryTicket ticket = recv.Result!;

        try
        {
            return new QuickClient(target, ticket, loginData);
        }
        catch (Exception ex)
        {
            Echo.LogError("Couldn't create client: " + ex.Message);
            return null;
        }
    }

    private QuickClient(IPEndPoint target, EntryTicket ticket, ServerLogin loginData)
    {
        var (nickname, password) = loginData;

        _udp = new UdpClient2(
            port: 0,
            isListener: false,
            isLoopback: IPAddress.IsLoopback(target.Address),
            isIPv6: target.AddressFamily == AddressFamily.InterNetworkV6,
            recvBufferSize: 256 * 1024,
            destination: target
            );

        Credentials credentials = new(
            nickname: nickname,
            password: password,
            serverSecret: ticket.ServerSecret,
            challengeId: ticket.ChallengeId,
            timestamp: ticket.Timestamp,
            runId: ticket.RunId
            );

        _rsa = new KeyRSA(ticket.RsaPublicKey.Bytes264);

        KeyAES aes = KeyAES.GenerateNew();
        byte[] aesBytes = aes.ExportKey();

        _conn = new QuickConnection(_udp, aes);
        _conn.SendHandshake(
            new AllowConnection(credentials, aesBytes), _rsa
            );
    }

    public void Send<T>(in T payload, bool safe = true) where T : unmanaged
    {
        _conn.Send(payload, safe);
    }

    public void OnReceive<T>(CmdHandler<T>? execute) where T : unmanaged
    {
        _listeners += () =>
        {
            if (_conn.TryCastCurrent(out T result))
            {
                execute?.Invoke(result);
            }
        };
    }

    public void Tick(float deltaTime)
    {
        while (_udp.TryReceive(out DataBox result))
        {
            byte[] bytes = result.Data;
            _conn.PushFromWeb(bytes);
        }

        _conn.Tick(deltaTime);

        while (_conn.MoveNext())
        {
            _listeners?.Invoke();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _conn?.Dispose();
        _rsa?.Dispose();
        _udp?.Dispose();
    }
}
