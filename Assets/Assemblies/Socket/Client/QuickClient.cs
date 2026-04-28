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
using Larnix.Socket.Security.KeyStructs;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Larnix.Socket.Client;

public class QuickClient : ITickable, IDisposable
{
    public long AvgRtt => _conn.AvgRtt; // ms
    public bool IsDead => _conn.IsDead;
    public IPEndPoint Target => _conn.Target; // modifying this may cause unexpected behaviour!

    private readonly UdpClient2 _udp;
    private readonly KeyRsa _rsa;
    private readonly KeyAes _aes;
    private readonly Connection _conn;

    private readonly ITargetedSocket _socket;

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

        _socket = new TargetedSocket(_udp, target);

        Credentials credentials = new(
            nickname: nickname,
            password: password,
            serverSecret: ticket.ServerSecret,
            challengeId: ticket.ChallengeId,
            timestamp: ticket.Timestamp,
            runId: ticket.RunId
            );

        _rsa = KeyRsa.FromPublicStruct(ticket.RsaPublicKey);
        _aes = KeyAes.GenerateNew();

        FixedAes aesKey = _aes.ExportKey();

        _conn = new Connection(_socket, aesKey);
        _conn.SendHandshake(
            new AllowConnection(credentials, aesKey), _rsa
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
        while (_socket.TryReceive(out byte[] bytes))
        {
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

        _conn.Dispose();
        _aes.Dispose();
        _rsa.Dispose();
        _udp.Dispose();
    }
}
