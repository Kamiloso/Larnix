using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Larnix.Socket.Channel;
using Larnix.Socket.Channel.Networking;
using Larnix.Socket.Packets;
using Larnix.Socket.Security.Keys;
using Larnix.Core.Utils;
using Larnix.Socket.Helpers.Limiters;
using Larnix.Core;
using Larnix.Socket.Packets.Control;
using Larnix.Core.DataStructures;

namespace Larnix.Socket.Backend
{
    internal class ConnDict : ITickable, IDisposable
    {
        public record PacketPair(HeaderSpan Packet, string Owner);

        public ushort MaxPlayers { get; }
        public ushort CurrentPlayers => (ushort)_connections.Count;

        private readonly INetworkInteractions _socket;
        private readonly QuickServer _server;

        private readonly BiMap<IPEndPoint, string> _nickAndEp = new();
        private readonly Dictionary<IPEndPoint, Connection> _connections = new();
        private readonly Dictionary<IPEndPoint, PreLoginBuffer> _preLogins = new();

        private readonly Limiter<InternetID> _connLimiter = new(3);
        private readonly PriorityQueue<PacketPair, int> _received = new();

        private bool _disposed = false;

        public ConnDict(QuickServer server, INetworkInteractions socket, ushort maxPlayers)
        {
            _server = server;
            _socket = socket;
            MaxPlayers = maxPlayers;
        }

        public bool TryGetEndPoint(string nickname, out IPEndPoint endPoint)
        {
            return _nickAndEp.TryGetKey(nickname, out endPoint);
        }

        public bool IsOnline(string nickname)
        {
            return TryGetEndPoint(nickname, out _);
        }

        public bool TryGetConnectionObject(IPEndPoint endPoint, out Connection connection)
        {
            return _connections.TryGetValue(endPoint, out connection);
        }

        public bool TryAddPreLogin(IPEndPoint endPoint, PayloadBox synBox)
        {
            if (CurrentPlayers < MaxPlayers && !_preLogins.ContainsKey(endPoint))
            {
                PreLoginBuffer preLogin = new PreLoginBuffer(synBox);
                _preLogins[endPoint] = preLogin;

                return true;
            }
            return false;
        }

        public bool TryPromoteConnection(IPEndPoint endPoint)
        {
            if (CurrentPlayers < MaxPlayers && _preLogins.TryGetValue(endPoint, out var preLogin))
            {
                AllowConnection allowConnection = preLogin.AllowConnection;

                string nickname = allowConnection.Nickname;
                KeyAES keyAES = new KeyAES(allowConnection.KeyAES);

                if (TryGetEndPoint(nickname, out var alreadyConnectedEp))
                {
                    Core.Debug.LogWarning($"Player {nickname} tried to connect, but is already connected.");

                    KickRequest(alreadyConnectedEp);
                    return false; // reject, player may connect once again
                }

                InternetID internetID = _server.MakeInternetID(endPoint);
                if (!_connLimiter.TryAdd(internetID))
                {
                    ulong max = _connLimiter.Max;
                    Core.Debug.LogWarning($"Network {internetID} has reached the limit of {max} simultaneous connections.\n" +
                        $"Cannot accept {nickname} while connecting from {endPoint}");
                    
                    return false; // reject, too many connections from internet ID
                }

                // ----- Real Promote Connection -----

                Connection newConn = Connection.CreateServer(_socket, endPoint, keyAES);

                byte[] bytes;
                while ((bytes = preLogin.TryReceive(out var isSyn)) != null)
                {
                    newConn.PushFromWeb(bytes, isSyn);
                }

                _nickAndEp.SetPair(endPoint, nickname);
                _connections.Add(endPoint, newConn);
                _preLogins.Remove(endPoint);

                return true;
            }
            return false;
        }

        public void PushFromWeb(IPEndPoint endPoint, byte[] incoming)
        {
            if (_connections.TryGetValue(endPoint, out var conn))
            {
                conn.PushFromWeb(incoming, false);
            }
            else if (_preLogins.TryGetValue(endPoint, out var preLogin))
            {
                preLogin.PushFromWeb(incoming);
            }
        }

        public void Tick(float deltaTime)
        {
            if (_received.Count > 0)
                throw new InvalidOperationException("Not all packets were flushed! Cannot tick.");

            List<IPEndPoint> shuffledEndpoints = _connections.Keys
                .OrderBy(_ => Common.Rand()).ToList();
            
            foreach (IPEndPoint endPoint in shuffledEndpoints)
            {
                var conn = _connections[endPoint];

                conn.Tick(deltaTime);
                while (conn.TryReceive(out var packet, out bool stopSignal))
                {
                    string nickname = _nickAndEp[endPoint];

                    PacketPair pair = new PacketPair(packet, nickname);
                    int priority = 0; // default priority

                    if (packet.ID == Payload.CmdID<AllowConnection>()) priority = -1; // higher
                    if (packet.ID == Payload.CmdID<Stop>()) priority = -2; // highest
                    
                    _received.Enqueue(pair, priority);

                    if (stopSignal) // delete connection
                    {
                        _nickAndEp.RemoveByKey(endPoint);
                        _connections.Remove(endPoint);

                        InternetID internetID = _server.MakeInternetID(endPoint);
                        _connLimiter.Remove(internetID);

                        conn.Dispose();
                        break;
                    }
                }
            }
        }

        public bool TryDequeuePacket(out PacketPair packetPair)
        {
            return _received.TryDequeue(out packetPair);
        }

        public void TrySendTo(IPEndPoint endPoint, Payload payload, bool safemode = true)
        {
            if (TryGetConnectionObject(endPoint, out var conn))
            {
                conn.Send(payload, safemode);
            }
        }

        public void SendToAll(Payload payload, bool safemode = true)
        {
            foreach (var conn in _connections.Values)
            {
                conn.Send(payload, safemode);
            }
        }

        public void DiscardIncoming(IPEndPoint endPoint)
        {
            _preLogins.Remove(endPoint);
        }

        public void KickRequest(IPEndPoint endPoint)
        {
            if (TryGetConnectionObject(endPoint, out var conn))
            {
                conn.Close();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                foreach (var conn in _connections.Values)
                {
                    conn?.Dispose();
                }
            }
        }
    }
}
