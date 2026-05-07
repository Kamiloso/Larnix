#nullable enable
using Larnix.Core;
using Larnix.Core.Collections;
using Larnix.Socket.Limiters;
using Larnix.Socket.Server.Utility;
using Larnix.Socket.Channel;
using Larnix.Socket.Networking;
using Larnix.Socket.Payload.Packets;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Tools;
using System;
using System.Collections.Generic;
using System.Net;

namespace Larnix.Socket.Server.Receivers;

internal class ConnReceiver : ITickable, IDisposable
{
    private readonly ISocket _socket;
    private readonly IClients _clients;
    private readonly IAsyncLogins _asyncLogins;
    private readonly Coroutines _coroutines;
    private readonly QuickSettings _settings;

    private readonly TrafficLimiter<string> _cidrLimiter;

    private readonly Dictionary<IPEndPoint, Connection> _conns = new();
    private readonly Dictionary<IPEndPoint, string> _cidrs = new();
    private readonly BiMap<IPEndPoint, string> _bimap = new();

    public ConnReceiver(ISocket socket, IClients clients, IAsyncLogins asyncLogins, Coroutines coroutines, QuickSettings settings)
    {
        _socket = socket;
        _clients = clients;
        _asyncLogins = asyncLogins;
        _coroutines = coroutines;
        _settings = settings;

        _cidrLimiter = new TrafficLimiter<string>(
            maxTrafficLocal: _settings.Security.Limiters.Connections.PerNetwork,
            maxTrafficGlobal: _settings.Security.Limiters.Connections.Global
            );
    }

    public void EstablishConnection(IPEndPoint target, string cidr, byte[] decrypted)
    {
        if (!NetworkSerializer.TryDecryptedBytesAs(
            decrypted, out _, out AllowConnection allowConnection))
            return;

        Credentials credentials = allowConnection.Credentials;
        bool isLoopback = IPAddress.IsLoopback(target.Address);

        _coroutines.Start(
            method: _asyncLogins.LoginOrRegister(credentials, isLoopback),
            onResult: success =>
            {
                string nickname = credentials.Nickname;

                if (!success) return;
                if (_bimap.ContainsValue(nickname)) return;
                if (_bimap.Count >= _settings.MaxPlayers) return;

                if (_cidrLimiter.TryAdd(cidr))
                {
                    Connection conn = new(
                        socket: new TargetedSocket(_socket, target),
                        aesKey: allowConnection.AesKey
                        );

                    _clients.AddClient(nickname, conn);

                    _conns.Add(target, conn);
                    _cidrs.Add(target, cidr);
                    _bimap.SetPair(target, nickname);
                }
            });
    }

    public void PushFromWeb(IPEndPoint target, byte[] data)
    {
        if (_bimap.TryGetValue(target, out string nickname))
        {
            _clients.PushFromWeb(nickname, data);
        }
    }

    public void Tick(float deltaTime)
    {
        List<IPEndPoint> toRemove = new();

        foreach (var (target, conn) in _conns)
        {
            conn.Tick(deltaTime);
            if (conn.IsDead)
            {
                toRemove.Add(target);
            }
        }

        foreach (var target in toRemove)
        {
            _conns[target].Dispose();

            string nickname = _bimap[target];
            _clients.RemoveClient(nickname);

            string cidr = _cidrs[target];
            _cidrLimiter.Remove(cidr);

            _conns.Remove(target);
            _cidrs.Remove(target);
            _bimap.RemoveByKey(target);
        }
    }

    public void Dispose()
    {
        foreach (var conn in _conns.Values)
        {
            conn.Dispose();
        }
    }
}
