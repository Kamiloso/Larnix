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
                    Disconnect(alreadyConnectedEp);
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

        private Queue<(HeaderSpan packet, string owner)> ReceiveAll()
        {
            Queue<(HeaderSpan, string)> received = new();

            foreach (IPEndPoint endPoint in _connections.Keys.OrderBy(_ => Common.Rand()))
            {
                Queue<HeaderSpan> packets = _connections[endPoint].Receive();
                string nickname = _epToNick[endPoint];

                while (packets.Count > 0)
                {
                    received.Enqueue((packets.Dequeue(), nickname));
                }
            }
            
            return received;
        }

        public Queue<(HeaderSpan packet, string owner)> TickAndReceive(float deltaTime)
        {
            Queue<(HeaderSpan, string)> received = ReceiveAll();

            foreach (var kvp in _connections.ToList())
            {
                var endPoint = kvp.Key;
                var connection = kvp.Value;

                connection.Tick(deltaTime);
                if (connection.IsDead)
                {
                    received.Enqueue((connection.GenerateStop(), _epToNick[endPoint]));
                    Disconnect(endPoint);
                }
            }
            return received;
        }

        public void Disconnect(IPEndPoint endPoint)
        {
            var state = ConnStateOf(endPoint);

            if (state == ConnState.Connected)
            {
                var nickname = _epToNick[endPoint];   
                if (_connections.TryGetValue(endPoint, out var conn))
                {
                    conn.Dispose();
                }

                _nickToEp.Remove(nickname);
                _epToNick.Remove(endPoint);
                _connections.Remove(endPoint);
            }
            else if (state == ConnState.PreLogin)
            {
                _preLogins.Remove(endPoint);
            }
            else return;
        }

        public void Dispose()
        {
            foreach (var endPoint in _connections.Keys.ToList())
            {
                Disconnect(endPoint);
            }
        }
    }
}
