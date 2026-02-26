using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using Larnix.Socket.Packets;
using System.Threading.Tasks;
using System;
using Larnix.Core.Utils;
using Larnix.Socket.Security.Keys;
using Larnix.Socket.Channel;
using Larnix.Socket.Packets.Control;
using Larnix.Socket.Channel.Networking;
using Larnix.Socket.Helpers;
using Larnix.Socket.Helpers.Limiters;
using Larnix.Core;
using PacketPair = Larnix.Socket.Backend.ConnDict.PacketPair;
using LoginMode = Larnix.Socket.Backend.UserManager.LoginMode;

namespace Larnix.Socket.Backend
{
    public class QuickServer : ITickable, IDisposable
    {
        public const string PRIVATE_KEY_FILENAME = "private_key.pem";
        public const string SERVER_SECRET_FILENAME = "server_secret.txt";

        // --- Public Properties ---
        public ushort Port => Config.Port;
        public string Authcode { get; }
        public ushort PlayerCount => (ushort)ConnDict.Count;
        public ushort PlayerLimit => Config.MaxClients;
        public IUserManager IUserManager => UserManager;

        // --- Internal Properties ---
        internal long ServerSecret { get; }
        internal long RunID { get; }
        internal QuickConfig Config { get; }
        internal UserManager UserManager { get; }
        internal ConnDict ConnDict { get; }

        // --- Private classes ---
        private readonly KeyRSA _keyRSA;
        private readonly TripleSocket _udpSocket;
        private readonly TrafficLimiter<InternetID> _heavyPacketLimiter;
        private readonly CycleTimer[] _cycleTimers;

        // --- Other ---
        private readonly Dictionary<CmdID, Action<HeaderSpan, string>> Subscriptions = new();
        private bool _disposed;

        public QuickServer(QuickConfig serverConfig)
        {
            // Nested classes
            _keyRSA = new KeyRSA(serverConfig.DataPath, PRIVATE_KEY_FILENAME); // 1
            _udpSocket = new TripleSocket(serverConfig.Port, serverConfig.IsLoopback); // 2
            ConnDict = new ConnDict(this, _udpSocket, serverConfig.MaxClients); // 3
            Config = serverConfig.WithPort(_udpSocket.Port);
            UserManager = new UserManager(this); // 4
            _heavyPacketLimiter = new TrafficLimiter<InternetID>(5, 50); // per second

            // Constants
            ServerSecret = Security.Authcode.ObtainSecret(serverConfig.DataPath, SERVER_SECRET_FILENAME);
            Authcode = Security.Authcode.ProduceAuthCodeRSA(_keyRSA.ExportPublicKey(), ServerSecret);
            RunID = Common.GetSecureLong();

            // Cycle timers
            _cycleTimers = new[]
            {
                new CycleTimer(1f, () => _heavyPacketLimiter.Reset()), // 1 second
                new CycleTimer(5f, () => _udpSocket.KeepAlive()), // 5 seconds
            };
        }

        public async Task<string> EstablishRelayAsync(string relayAddress)
        {
            ushort? relayPort = await _udpSocket.StartRelayAsync(relayAddress);
                
            if (relayPort != null)
            {
                string address = Common.FormatUdpAddress(relayAddress, relayPort.Value);
                Core.Debug.LogSuccess("Connected to relay!");
                Core.Debug.Log("Address: " + address);
                return address;
            }
            else
            {
                Core.Debug.LogWarning("Cannot connect to relay!");
                return null;
            }
        }

        public void Tick(float deltaTime)
        {
            // Tick cycle timers
            foreach (var timer in _cycleTimers)
            {
                timer.Tick(deltaTime);
            }

            // Interpret bytes from socket
            while (_udpSocket.TryReceive(out var item))
            {
                IPEndPoint remoteEP = item.target;
                byte[] bytes = item.data;

                InterpretBytes(remoteEP, bytes);
            }

            // Tick & Receive from connections
            Queue<PacketPair> packets = ConnDict.TickAndReceive(deltaTime);
            while (packets.Count > 0)
            {
                var element = packets.Dequeue();
                var packet = element.Packet;
                string owner = element.Owner;

                if (Subscriptions.TryGetValue(packet.ID, out var Execute))
                {
                    Execute(packet, owner);
                }
            }

            // Tick user manager (coroutines inside)
            UserManager.Tick(deltaTime);
        }

        private void InterpretBytes(IPEndPoint target, byte[] bytes)
        {
            InternetID internetID = MakeInternetID(target);
            if (internetID.IsClassE)
            {
                return; // class E is used internally
            }

            if (PayloadBox.TryDeserializeHeader(bytes, out var header))
            {
                // Heavy packet limiter
                if (header.HasFlag(PacketFlag.SYN) ||
                    header.HasFlag(PacketFlag.RSA) ||
                    header.HasFlag(PacketFlag.NCN))
                {
                    if (!_heavyPacketLimiter.TryAdd(internetID))
                        return; // drop heavy packet
                }

                // Decrypt RSA
                if (header.HasFlag(PacketFlag.RSA))
                {
                    if (!PayloadBox.TryDeserialize(bytes, _keyRSA, out var decrypted))
                        return; // drop invalid RSA packet
                    
                    decrypted.UnsetFlag(PacketFlag.RSA);
                    bytes = decrypted.Serialize(KeyEmpty.GetInstance());
                }

                if (header.HasFlag(PacketFlag.NCN))
                {
                    // Non-connection packet - NCN
                    if (PayloadBox.TryDeserialize(bytes, KeyEmpty.GetInstance(), out var box))
                        ProcessNCN(target, box.SeqNum, new HeaderSpan(box.Bytes));
                }
                else
                {
                    if (header.HasFlag(PacketFlag.SYN))
                    {
                        // Start new connection
                        if (PayloadBox.TryDeserialize(bytes, KeyEmpty.GetInstance(), out var synBox) &&
                            Payload.TryConstructPayload<AllowConnection>(synBox.Bytes, out var allowcon) &&
                            ConnDict.TryAddPreLogin(target, synBox))
                        {
                            P_LoginTry logtry = allowcon.ToLoginTry();
                            UserManager.StartLogin(target, logtry, LoginMode.Establishment);
                        }
                    }
                    else
                    {
                        // Established connection packet
                        ConnDict.EnqueueReceivedPacket(target, bytes);
                    }
                }
            }
        }

        private void ProcessNCN(IPEndPoint target, int ncnID, HeaderSpan headerSpan)
        {
            void SendNCN(Payload packet)
            {
                PayloadBox safeAnswer = new PayloadBox(
                    seqNum: ncnID,
                    ackNum: 0,
                    flags: (byte)PacketFlag.NCN,
                    payload: packet
                    );

                // answer always as plaintext
                byte[] payload = safeAnswer.Serialize(KeyEmpty.GetInstance());
                _udpSocket.Send(target, payload);
            };

            if (Payload.TryConstructPayload<P_ServerInfo>(headerSpan, out var infoask))
            {
                string nickname = infoask.Nickname;
                
                A_ServerInfo srvInfo = new A_ServerInfo(
                    publicKey: _keyRSA.ExportPublicKey(),
                    currentPlayers: PlayerCount,
                    maxPlayers: PlayerLimit,
                    gameVersion: Core.Version.Current,
                    challengeID: UserManager.GetChallengeID(nickname),
                    timestamp: Timestamp.GetTimestamp(),
                    runID: RunID,
                    motd: Config.Motd,
                    hostUser: Config.HostUser,
                    mayRegister: Config.AllowRegistration
                    );

                SendNCN(srvInfo);
            }

            else if (Payload.TryConstructPayload<P_LoginTry>(headerSpan, out var logtry))
            {
                string nickname = logtry.Nickname;

                var loginMode = logtry.IsPasswordChange() ?
                    LoginMode.PasswordChange : LoginMode.Discovery;
                
                UserManager.StartLogin(target, logtry, loginMode, success =>
                {
                    SendNCN(new A_LoginTry(success));
                });
            }
        }

        /// <summary>
        /// AllowConnection packet always starts the connection.
        /// Stop packet always ends the connection.
        /// Stop packet can only appear once in a returned packet queue.
        /// Stop packet may appear randomly, alone without AllowConnection. !!!
        /// </summary>
        public void Subscribe<T>(Action<T, string> InterpretPacket) where T : Payload, new()
        {
            CmdID ID = Payload.CmdID<T>();
            Subscriptions[ID] = (HeaderSpan headerSpan, string owner) =>
            {
                if (Payload.TryConstructPayload<T>(headerSpan, out var message))
                {
                    InterpretPacket(message, owner);
                }
            };
        }

        public bool IsActiveConnection(string nickname)
        {
            return TryGetClientEndPoint(nickname, out _);
        }

        public bool TryGetClientEndPoint(string nickname, out IPEndPoint endPoint)
        {
            IPEndPoint endPoint1 = ConnDict.EndPointOf(nickname);
            if (endPoint1 != null)
            {
                endPoint = endPoint1;
                return true;
            }
            endPoint = null;
            return false;
        }

        public void Send(string nickname, Payload packet, bool safemode = true)
        {
            if (TryGetClientEndPoint(nickname, out IPEndPoint endPoint))
            {
                ConnDict.SendTo(endPoint, packet, safemode);
            }
        }

        public void Broadcast(Payload packet, bool safemode = true)
        {
            ConnDict.SendToAll(packet, safemode);
        }

        public float GetPing(string nickname)
        {
            if (TryGetClientEndPoint(nickname, out IPEndPoint endPoint))
            {
                Connection conn = ConnDict.GetConnectionObject(endPoint);
                return conn.AvgRTT;
            }
            return 0f;
        }

        public void KickRequest(string nickname)
        {
            if (TryGetClientEndPoint(nickname, out IPEndPoint endPoint))
            {
                ConnDict.KickRequest(endPoint);
            }
        }

        internal InternetID MakeInternetID(IPEndPoint endPoint)
        {
            return new InternetID(
                endPoint.Address,
                endPoint.AddressFamily == AddressFamily.InterNetwork ?
                    Config.MaskIPv4 : Config.MaskIPv6
                );
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                UserManager?.Dispose(); // 4
                ConnDict?.Dispose(); // 3
                _udpSocket?.Dispose(); // 2
                _keyRSA?.Dispose(); // 1
            }
        }
    }
}
