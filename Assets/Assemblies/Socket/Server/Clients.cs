#nullable enable
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Socket.Channel;
using Larnix.Socket.Payload;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Socket.Server;

internal interface IClients
{
    ushort Count { get; }
    void AddClient(string nickname, Connection connection);
    bool RemoveClient(string nickname);
    bool HasClient(string nickname);
    void PushFromWeb(string nickname, byte[] data);
    void KickRequest(string nickname, Action? onKick = null);
}

internal class Clients : IClients, ITickable
{
    public ushort Count => (ushort)_conns.Count;

    private readonly Dictionary<string, Connection> _conns = new();
    private readonly Dictionary<string, List<Action>> _kickCallbacks = new();

    private event Action<string, Connection>? _listeners;

    private event ConnectedHandler? _onClientStart; // start auto-generated
    private event DisconnectedHandler? _onClientStop; // stop auto-generated

    public void AddClient(string nickname, Connection connection)
    {
        if (Count >= ushort.MaxValue)
            throw new InvalidOperationException("Cannot add more clients, maximum limit reached.");

        if (_conns.Values.Any(conn => ReferenceEquals(conn, connection)))
            throw new InvalidOperationException("Unable to add the same connection twice.");

        _conns.Add(nickname, connection);
        _onClientStart?.Invoke(nickname, connection.Target);
    }

    public bool RemoveClient(string nickname)
    {
        bool removed = false;

        IfHas(nickname, conn =>
        {
            if (!conn.IsDead)
                throw new InvalidOperationException("Cannot remove still running connection.");

            _conns.Remove(nickname);

            if (_kickCallbacks.TryGetValue(nickname, out List<Action> onKickList))
            {
                _kickCallbacks.Remove(nickname);
                onKickList.ForEach(action => action());
            }

            _onClientStop?.Invoke(nickname);

            removed = true;
        });

        return removed;
    }

    public bool HasClient(string nickname)
    {
        return _conns.ContainsKey(nickname);
    }

    public void PushFromWeb(string nickname, byte[] data)
    {
        IfHas(nickname, conn => conn.PushFromWeb(data));
    }

    public void KickRequest(string nickname, Action? onKick = null)
    {
        IfHas(nickname, conn =>
        {
            if (!_kickCallbacks.ContainsKey(nickname))
                _kickCallbacks[nickname] = new List<Action>();

            if (onKick != null)
                _kickCallbacks[nickname].Add(onKick);

            conn.Close();
        });
    }

    public void Send<T>(string nickname, in T payload, bool safemode) where T : unmanaged
    {
        if (_conns.TryGetValue(nickname, out var conn))
        {
            conn.Send(payload, safemode);
        }
    }

    public void Broadcast<T>(in T payload, bool safemode) where T : unmanaged
    {
        foreach (var conn in _conns.Values)
        {
            conn.Send(payload, safemode);
        }
    }

    public void OnConnected(ConnectedHandler? execute)
    {
        _onClientStart += execute;
    }

    public void OnDisconnected(DisconnectedHandler? execute)
    {
        _onClientStop += execute;
    }

    public void OnReceive<T>(CmdSenderHandler<T>? execute) where T : unmanaged
    {
        _listeners += (nickname, conn) =>
        {
            if (conn.TryCastCurrent(out T result))
            {
                execute?.Invoke(result, nickname);
            }
        };
    }

    public void Tick(float deltaTime)
    {
        List<string> nicknames = _conns.Keys
            .OrderBy(_ => RandUtils.NextInt())
            .ToList();

        foreach (string nickname in nicknames)
        {
            IfHas(nickname, conn =>
            {
                conn.Tick(deltaTime);

                while (conn.MoveNext())
                {
                    _listeners?.Invoke(nickname, conn);
                }
            });
        }
    }

    private void IfHas(string nickname, Action<Connection> action)
    {
        if (_conns.TryGetValue(nickname, out var conn))
        {
            action.Invoke(conn);
        }
    }
}
