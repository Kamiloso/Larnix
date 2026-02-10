using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Larnix.Socket.Backend;
using Larnix.Socket.Channel;
using Larnix.Socket.Channel.Networking;
using Larnix.Socket.Packets;
using Larnix.Socket.Security.Keys;
using Larnix.Core.Utils;

namespace Larnix.Socket
{
    internal record PacketPair(HeaderSpan Packet, string Owner);
    internal class ConnDict : IDisposable
    {
        // --- Properties ---
        public readonly ushort MAX_PLAYERS;
        public int Count => _connections.Count;

        public IEnumerable<string> Nicknames => _nickToEp.Keys;
        public IEnumerable<IPEndPoint> EndPoints => _epToNick.Keys;

        private readonly INetworkInteractions _socket;

        private readonly Dictionary<string, IPEndPoint> _nickToEp = new();
        private readonly Dictionary<IPEndPoint, string> _epToNick = new();
        private readonly Dictionary<IPEndPoint, Connection> _connections = new();
        private readonly Dictionary<IPEndPoint, PreLoginBuffer> _preLogins = new();

        private bool _disposed = false;

        // --- Informative ---
        public enum ConnState { None, PreLogin, Connected }
        public ConnState ConnStateOf(IPEndPoint endPoint)
        {
            if (_connections.ContainsKey(endPoint))
                return ConnState.Connected;

            if (_preLogins.ContainsKey(endPoint))
                return ConnState.PreLogin;
            
            return ConnState.None;
        }

        public string NicknameOf(IPEndPoint endPoint) => _epToNick.TryGetValue(endPoint, out var nickname) ? nickname : null;
        public IPEndPoint EndPointOf(string nickname) => _nickToEp.TryGetValue(nickname, out var endPoint) ? endPoint : null;

        // --- Functional ---
        public ConnDict(INetworkInteractions socket, ushort maxPlayers)
        {
            _socket = socket;
            MAX_PLAYERS = maxPlayers;
        }

        public bool TryAddPreLogin(IPEndPoint endPoint, PayloadBox synBox)
        {
            if (Count < MAX_PLAYERS && !_preLogins.ContainsKey(endPoint))
            {
                PreLoginBuffer preLogin = new PreLoginBuffer(synBox);
                _preLogins[endPoint] = preLogin;

                return true;
            }
            return false;
        }

        public bool TryPromoteConnection(IPEndPoint endPoint)
        {
            if (Count < MAX_PLAYERS && _preLogins.TryGetValue(endPoint, out var preLogin))
            {
                var nickname = preLogin.AllowConnection.Nickname;
                var keyAES = new KeyAES(preLogin.AllowConnection.KeyAES);
                var connection = Connection.CreateServer(_socket, endPoint, keyAES);

                IPEndPoint alreadyConnectedEp = EndPointOf(nickname);
                if (alreadyConnectedEp != null)
                {
                    KickRequest(alreadyConnectedEp);
                    return false; // reject, player may connect once again anyway (rewriting this code to accept would be a nightmare)
                }

                byte[] bytes;
                while ((bytes = preLogin.TryReceive(out var isSyn)) != null)
                {
                    connection.PushFromWeb(bytes, isSyn);
                }

                _epToNick[endPoint] = nickname;
                _nickToEp[nickname] = endPoint;
                
                _connections[endPoint] = connection;
                _preLogins.Remove(endPoint);

                return true;
            }
            return false;
        }

        public Connection GetConnectionObject(IPEndPoint endPoint)
        {
            if (_connections.TryGetValue(endPoint, out var connection))
            {
                return connection;
            }
            return null;
        }

        public void SendTo(IPEndPoint endPoint, Payload payload, bool safemode = true)
        {
            if (_connections.TryGetValue(endPoint, out var connection))
            {
                connection.Send(payload, safemode);
            }
        }

        public void SendToAll(Payload payload, bool safemode = true)
        {
            foreach (var connection in _connections.Values)
            {
                connection.Send(payload, safemode);
            }
        }

        public void EnqueueReceivedPacket(IPEndPoint endPoint, byte[] incoming)
        {
            switch (ConnStateOf(endPoint))
            {
                case ConnState.Connected:
                    _connections[endPoint].PushFromWeb(incoming);
                    break;

                case ConnState.PreLogin:
                    _preLogins[endPoint].PushFromWeb(incoming);
                    break;
            }
        }

        public Queue<PacketPair> TickAndReceive(float deltaTime)
        {
            Queue<PacketPair> received = new();

            foreach (IPEndPoint endPoint in _connections.Keys.OrderBy(_ => Common.Rand()))
            {
                var connection = _connections[endPoint];

                connection.Tick(deltaTime);
                if (!connection.IsDead) // receive all
                {
                    Queue<HeaderSpan> recv = connection.Receive();
                    string nickname = _epToNick[endPoint];

                    while (recv.Count > 0)
                    {
                        received.Enqueue(new PacketPair(recv.Dequeue(), nickname));
                    }
                }
                else // terminate connection if dead
                {
                    received.Enqueue(new PacketPair(
                        Packet: connection.GenerateStop(),
                        Owner: _epToNick[endPoint])
                        );
                    
                    var nickname = _epToNick[endPoint];
                    if (_connections.TryGetValue(endPoint, out var conn))
                    {
                        conn.Dispose();
                    }

                    _nickToEp.Remove(nickname);
                    _epToNick.Remove(endPoint);
                    _connections.Remove(endPoint);
                }
            }
            return received;
        }

        public void DiscardIncoming(IPEndPoint endPoint)
        {
            if (ConnStateOf(endPoint) == ConnState.PreLogin)
            {
                _preLogins.Remove(endPoint);
            }
        }

        public void KickRequest(IPEndPoint endPoint)
        {
            if (_connections.TryGetValue(endPoint, out var conn))
            {
                conn.Dispose();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                foreach (var conn in _connections.Values)
                {
                    conn.Dispose();
                }
            }
        }
    }
}
