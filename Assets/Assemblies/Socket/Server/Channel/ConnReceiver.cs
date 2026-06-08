#nullable enable
using Larnix.Core;
using Larnix.Core.Collections;
using Larnix.Socket.Session;
using Larnix.Socket.Networking;
using Larnix.Socket.Payload.Packets;
using Larnix.Socket.Payload.Structs;
using System;
using System.Collections.Generic;
using System.Net;
using Larnix.Socket.Session.Tools;
using Larnix.Socket.Server.Users;
using Larnix.Socket.Server.Configuration;

namespace Larnix.Socket.Server.Channel;

internal class ConnReceiver : ITickable, IDisposable
{
    private readonly ISocket _socket;
    private readonly QuickSettings _settings;
    private readonly Coroutines _coroutines;
    private readonly Limiters _limiters;
    private readonly ClientSessions _clients;
    private readonly AsyncLogins _asyncLogins;

    private readonly Dictionary<IPEndPoint, Connection> _conns = new();
    private readonly Dictionary<IPEndPoint, string> _cidrs = new();
    private readonly BiMap<IPEndPoint, string> _bimap = new();

    public ConnReceiver(MainServices services, ClientSessions clients, AsyncLogins asyncLogins)
    {
        _socket = services.Socket;
        _settings = services.Settings;
        _coroutines = services.Coroutines;
        _limiters = services.Limiters;
        _clients = clients;
        _asyncLogins = asyncLogins;
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

                if (_limiters.ConcurrentSessions.TryAdd(cidr))
                {
                    Connection conn = new(
                        socket: new TargetedSocket(_socket, target),
                        aes: allowConnection.AesKey.GetKey()
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
            _limiters.ConcurrentSessions.Remove(cidr);

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
