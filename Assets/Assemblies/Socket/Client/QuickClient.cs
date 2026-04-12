#nullable enable
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System;
using System.Threading.Tasks;
using Larnix.Socket.Security.Keys;
using Larnix.Socket.Networking;
using Larnix.Model;
using Larnix.Core;
using Larnix.Socket.Requests;
using Larnix.Socket.Helpers.Records;
using Larnix.Socket.Payload.Packets;
using Larnix.Socket.Payload.Structs;

namespace Larnix.Socket.Client;

public class QuickClient : IDisposable, ITickable
{
    public float Ping => _conn.AvgRTT * 1000f; // ms
    public bool IsDead => _conn.IsDead;

    private readonly UdpClient2 _udp;
    private readonly Connection _conn;
    private readonly KeyRSA _rsaKey;

    private readonly Dictionary<CmdID, Action<HeaderSpan>> Subscriptions = new();

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

        KeyAES keyAes = KeyAES.GenerateNew();
        byte[] aesBytes = keyAes.ExportKey();

        _rsaKey = new KeyRSA(ticket.RsaPublicKey.Bytes264);

        Credentials credentials = new(
            nickname: nickname,
            password: password,
            serverSecret: ticket.ServerSecret,
            challengeId: ticket.ChallengeId,
            timestamp: ticket.Timestamp,
            runId: ticket.RunId
            );

        AllowConnection synPacket = new(credentials, aesBytes);

        _conn = Connection.CreateClient(_udp, target, keyAes, _rsaKey, synPacket);
    }

    public void Tick(float deltaTime)
    {
        // Get packets from UDP client
        while (_udp.TryReceive(out var pair))
        {
            _conn.PushFromWeb(pair.data, false);
        }

        // Process received packets
        _conn.Tick(deltaTime);
        if (!_conn.IsDead)
        {
            while (_conn.TryReceive(out var headerSpan, out bool stopSignal))
            {
                CmdID cmdID = headerSpan.ID;
                if (Subscriptions.TryGetValue(cmdID, out var Execute))
                {
                    Execute(headerSpan);
                }

                if (stopSignal)
                {
                    break;
                }
            }
        }
    }

    public void Subscribe<T>(Action<T> InterpretPacket) where T : Payload_Legacy
    {
        CmdID ID = Payload_Legacy.CmdID<T>();
        Subscriptions[ID] = (HeaderSpan packetBytes) =>
        {
            if (Payload_Legacy.TryConstructPayload<T>(packetBytes, out var message))
            {
                InterpretPacket(message);
            }
        };
    }

    public void Send(Payload_Legacy packet, bool safemode = true)
    {
        _conn.Send(packet, safemode);
    }

    public void Dispose()
    {
        _conn?.Dispose();
        _rsaKey?.Dispose();
        _udp?.Dispose();
    }
}
