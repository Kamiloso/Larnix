using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using Larnix.Socket.Packets;
using System.Threading.Tasks;
using System;
using System.Threading;
using Larnix.Core.Utils;
using Larnix.Socket.Security;
using Larnix.Socket.Channel;
using Larnix.Socket.UdpClients;

namespace Larnix.Socket.Backend
{
    public class QuickServer : IDisposable
    {
        public const string LoopbackOnlyNickname = "Player";
        public const string LoopbackOnlyPassword = "SGP_PASSWORD\x01";

        public readonly ushort Port, MaxClients;
        public readonly bool IsLoopback;
        public readonly string DataPath;
        public readonly uint GameVersion;
        public readonly string UserText1, UserText2, UserText3;

        public readonly long Secret;
        public readonly string Authcode;

        internal readonly long RunID;

        public bool CanReceive { get; set; } = true;

        private bool InitializedMasks = false;
        internal int MaskIPv4 = 32;
        internal int MaskIPv6 = 56;

        public readonly UserManager UserManager;
        public readonly RSA KeyRSA;

        private readonly DoubleSocket DoubleSocket;
        private readonly ManagerNCN FastMessages;
        internal readonly HashSet<string> ReservedNicknames = new();

        private readonly Dictionary<string, Connection> connections = new();
        private readonly Dictionary<IPEndPoint, Connection> connectionsByEndPoint = new();

        private readonly Dictionary<IPEndPoint, PreLoginBuffer> PreLoginBuffers = new();
        private readonly Dictionary<CmdID, Action<Packet, string>> Subscriptions = new();

        private const float CON_RESET_TIME = 1f; // seconds
        private float timeToResetCon = CON_RESET_TIME;
        private uint MAX_CON = 5; // limit heavy packets per IP mask (internetID)
        private uint MAX_GLOBAL_CON = 50; // limit heavy packets globally
        private Dictionary<InternetID, uint> recentConCount = new();
        private uint globalConCount = 0;

        private int _relaySubmitted = 0; // 0 - false, 1 - true
        private RelayConnection Relay = null;

        private const float KEEP_ALIVE_PERIOD = 5f;
        private float relayTimeToKeepAlive = KEEP_ALIVE_PERIOD;

        public static QuickServer CreateServerSync(ushort port, ushort maxClients, bool isLoopback, string dataPath, IUserAPI userAPI, uint gameVersion,
            string userText1 = "",
            string userText2 = "",
            string userText3 = ""
            )
        {
            return new QuickServer(port, maxClients, isLoopback, dataPath, userAPI, gameVersion,
                userText1, userText2, userText3);
        }

        private QuickServer(ushort port, ushort maxClients, bool isLoopback, string dataPath, IUserAPI userAPI, uint gameVersion,
            string userText1 = "",
            string userText2 = "",
            string userText3 = ""
            )
        {
            if (!Validation.IsGoodText<String256>(userText1) ||
                !Validation.IsGoodText<String256>(userText2) ||
                !Validation.IsGoodText<String256>(userText3))
                throw new ArgumentException("Wrong UserText format! Cannot be larger than 128 characters or end with NULL (0x00).");

            // managed objects
            DoubleSocket = new DoubleSocket(port, isLoopback);
            FastMessages = new ManagerNCN(this);
            UserManager = new UserManager(userAPI);
            KeyRSA = KeyObtainer.ObtainKeyRSA(dataPath);

            // other configuration
            Port = DoubleSocket.Port;
            MaxClients = maxClients;
            IsLoopback = isLoopback;
            DataPath = dataPath;
            Secret = KeyObtainer.ObtainSecret(dataPath);
            Authcode = Security.Authcode.ProduceAuthCodeRSA(KeyObtainer.KeyToPublicBytes(KeyRSA), Secret);
            GameVersion = gameVersion;
            UserText1 = userText1;
            UserText2 = userText2;
            UserText3 = userText3;

            // run random
            RunID = KeyObtainer.GetSecureLong();
        }

        public void ConfigureMasks(int maskIPv4, int maskIPv6)
        {
            if (InitializedMasks)
                throw new InvalidOperationException("Masks were already initialized! " +
                    "This method should be called from directly after QuickServer constructor.");

            InitializedMasks = true;
            MaskIPv4 = maskIPv4;
            MaskIPv6 = maskIPv6;
        }

        public async Task<ushort?> ConfigureRelay(string relayAddress)
        {
            if (Interlocked.CompareExchange(ref _relaySubmitted, 1, 0) != 0)
                throw new InvalidOperationException("Cannot double-configure relay! Please, restart the server and try again.");

            Relay = await RelayConnection.EstablishRelayAsync(relayAddress);
            return Relay?.RemotePort;
        }

        private void RememberConnection(string nickname, Connection conn)
        {
            connections.Add(nickname, conn);
            connectionsByEndPoint.Add(conn.EndPoint, conn);
        }

        private void ForgetConnection(string nickname)
        {
            IPEndPoint endPoint = connections[nickname].EndPoint;
            connections.Remove(nickname);
            connectionsByEndPoint.Remove(endPoint);
        }

        private bool CanAcceptSYN(IPEndPoint endPoint, string nickname)
        {
            if (connections.Count >= MaxClients)
                return false;

            return !connections.Any(kvp =>
                kvp.Key == nickname || kvp.Value.EndPoint.Equals(endPoint));
        }

        internal void LoginAccept(IPEndPoint remoteEP)
        {
            if (!PreLoginBuffers.ContainsKey(remoteEP))
                throw new InvalidOperationException("Couldn't find login request to accept.");

            PreLoginBuffer preLoginBuffer = PreLoginBuffers[remoteEP];
            AllowConnection allowConnection = preLoginBuffer.AllowConnection;
            string nickname = allowConnection.Nickname;

            if (!CanAcceptSYN(remoteEP, nickname))
            {
                LoginDeny(remoteEP);
                return;
            }

            Action<byte[]> SendData;
            if (IsRelayAddress(remoteEP.Address)) // relay socket
            {
                SendData = bytes => Relay.Send(remoteEP, bytes);
            }
            else // normal double socket
            {
                SendData = bytes => DoubleSocket.Send(remoteEP, bytes);
            }

            Connection connection = new Connection(SendData, remoteEP, allowConnection.KeyAES);

            RememberConnection(nickname, connection);
            PreLoginBuffers.Remove(remoteEP);

            bool hasSyn = true;
            byte[] bytes;
            while ((bytes = preLoginBuffer.Pop()) != null)
            {
                connection.PushFromWeb(bytes, hasSyn);
                hasSyn = false;
            }
        }

        internal void LoginDeny(IPEndPoint remoteEP)
        {
            if (!PreLoginBuffers.ContainsKey(remoteEP))
                throw new InvalidOperationException("Couldn't find login request to deny.");

            PreLoginBuffers.Remove(remoteEP);
        }

        public void ServerTick(float deltaTime)
        {
            // Block initializing masks
            InitializedMasks = true;

            // Tick database
            FastMessages.Tick(deltaTime);

            // Interpret bytes from socket
            while (DoubleSocket.TryReceive(out var item))
            {
                IPEndPoint remoteEP = item.endPoint;
                byte[] bytes = item.data;

                InterpretBytes(remoteEP, bytes);
            }

            // Interpret bytes from relay
            while (Relay?.TryReceive(out var item) == true)
            {
                IPEndPoint remoteEP = item.endPoint;
                byte[] bytes = item.data;

                InterpretBytes(remoteEP, bytes);
            }

            // Tick every connection
            foreach (var conn in connections.Values)
            {
                conn.Tick(deltaTime);
            }

            // Randomize client order
            string[] nicknames = connections.Keys.ToArray();
            Shuffle(nicknames);

            // Receive packets
            Queue<(Packet, string)> packetList = new();
            foreach (string nickname in nicknames)
            {
                Connection conn = connections[nickname];
                Queue<Packet> packets = conn.Receive();
                while (packets.Count > 0)
                    packetList.Enqueue((packets.Dequeue(), nickname));
            }

            // Remove dead connections
            foreach (string nickname in connections.Keys.ToList())
            {
                Connection conn = connections[nickname];
                if (conn.IsDead)
                {
                    // add finishing message
                    Packet packet = new Stop(0);
                    packetList.Enqueue((packet, nickname));

                    // reset player slots
                    ForgetConnection(nickname);
                }
            }

            // Reset RSA counter
            timeToResetCon -= deltaTime;
            if(timeToResetCon < 0)
            {
                timeToResetCon = CON_RESET_TIME;
                recentConCount.Clear();
                globalConCount = 0;
            }

            // Relay keep alive
            relayTimeToKeepAlive -= deltaTime;
            if (relayTimeToKeepAlive < 0)
            {
                relayTimeToKeepAlive = KEEP_ALIVE_PERIOD;
                Relay?.KeepAlive();
            }

            // Interpret packets
            while (packetList.Count > 0)
            {
                var element = packetList.Dequeue();
                Packet packet = element.Item1;
                string owner = element.Item2;

                if (packet != null && Subscriptions.TryGetValue(packet.ID, out var Execute))
                {
                    Execute(packet, owner);
                }
            }
        }

        private void InterpretBytes(IPEndPoint remoteEP, byte[] bytes)
        {
            QuickPacket header = new QuickPacket();
            if (header.TryDeserialize(bytes, true))
            {
                InternetID internetID = new InternetID(
                    remoteEP.Address,
                    remoteEP.AddressFamily == AddressFamily.InterNetwork ? MaskIPv4 : MaskIPv6
                    );
                uint conCount = recentConCount.ContainsKey(internetID) ? recentConCount[internetID] : 0;

                // Limit packets with specific flags
                if (header.HasFlag(PacketFlag.SYN) ||
                    header.HasFlag(PacketFlag.RSA) ||
                    header.HasFlag(PacketFlag.NCN))
                {
                    if (conCount < MAX_CON) recentConCount[internetID] = ++conCount;
                    else return;

                    if (globalConCount < MAX_GLOBAL_CON) globalConCount++;
                    else return;
                }

                if (header.HasFlag(PacketFlag.RSA)) // encrypted with RSA
                {
                    if (KeyRSA != null)
                    {
                        // Deserialize with decryption and serialize without encryption
                        QuickPacket middlePacket = new QuickPacket();
                        middlePacket.Encryption = bytes => Encryption.DecryptRSA(bytes, KeyRSA);
                        if (!middlePacket.TryDeserialize(bytes))
                            return;

                        middlePacket.Encryption = null;
                        bytes = middlePacket.Serialize();
                    }
                    else return;
                }

                if (header.HasFlag(PacketFlag.NCN)) // fast question, fast answer
                {
                    // Check packet type and answer properly
                    QuickPacket ncnPacket = new QuickPacket();
                    if (!ncnPacket.TryDeserialize(bytes))
                        return;

                    FastMessages.ProcessNCN(remoteEP, ncnPacket.SeqNum, ncnPacket.Packet);
                }
                else
                {
                    if (header.HasFlag(PacketFlag.SYN)) // start connection
                    {
                        QuickPacket safeSynPacket = new QuickPacket();
                        if (!safeSynPacket.TryDeserialize(bytes))
                            return;

                        if (Payload.TryConstructPayload<AllowConnection>(safeSynPacket.Packet, out var allowcon))
                        {
                            string nickname = allowcon.Nickname;
                            string password = allowcon.Password;
                            long serverSecret = allowcon.ServerSecret;
                            long challengeID = allowcon.ChallengeID;
                            long timestamp = allowcon.Timestamp;
                            long runID = allowcon.RunID;

                            if (!CanAcceptSYN(remoteEP, nickname))
                                return;

                            if (!PreLoginBuffers.ContainsKey(remoteEP))
                            {
                                PreLoginBuffer preLoginBuffer = new PreLoginBuffer(allowcon);
                                preLoginBuffer.Push(bytes);
                                PreLoginBuffers.Add(remoteEP, preLoginBuffer);

                                FastMessages.TryLogin(remoteEP, nickname, password, serverSecret, challengeID, timestamp, runID);
                            }
                        }
                        else return;
                    }
                    else // receive connection packet
                    {
                        if (PreLoginBuffers.TryGetValue(remoteEP, out var preBuffer))
                        {
                            preBuffer.Push(bytes);
                        }
                        else
                        {
                            if (connectionsByEndPoint.TryGetValue(remoteEP, out var conn))
                            {
                                conn.PushFromWeb(bytes);
                            }
                        }
                    }
                }
            }
        }

        public void Subscribe<T>(Action<T, string> InterpretPacket) where T : Payload, new()
        {
            CmdID ID = Payload.CmdID<T>();
            Subscriptions[ID] = (Packet packet, string owner) =>
            {
                if(Payload.TryConstructPayload<T>(packet, out var message))
                {
                    InterpretPacket(message, owner);
                }
            };
        }

        public void Send(string nickname, Packet packet, bool safemode = true)
        {
            if (connections.TryGetValue(nickname, out var conn))
            {
                conn.Send(packet, safemode);
            }
        }

        public void Broadcast(Packet packet, bool safemode = true)
        {
            foreach (var conn in connections.Values.ToList())
            {
                conn.Send(packet, safemode);
            }
        }

        public void FinishConnection(string nickname)
        {
            if (connections.TryGetValue(nickname, out var conn))
            {
                conn.FinishConnection();
                ForgetConnection(nickname);
            }
        }

        public void FinishAllConnections()
        {
            foreach (string nickname in connections.Keys.ToList())
            {
                FinishConnection(nickname);
            }
        }

        internal void SendNCN(IPEndPoint endPoint, int ncnID, Packet packet)
        {
            QuickPacket safeAnswer = new QuickPacket(
                ncnID,
                0,
                (byte)PacketFlag.NCN,
                packet
                );

            byte[] sendBytes = safeAnswer.Serialize();
            if (IsRelayAddress(endPoint.Address)) // relay socket
            {
                Relay?.Send(endPoint, sendBytes);
            }
            else // normal double socket
            {
                DoubleSocket.Send(endPoint, sendBytes);
            }
        }

        public IPEndPoint GetClientEndPoint(string nickname)
        {
            if (connections.TryGetValue(nickname, out var conn))
                return conn.EndPoint;
            return null;
        }

        public static bool IsRelayAddress(IPAddress address)
        {
            return address.AddressFamily == AddressFamily.InterNetwork && ((address.GetAddressBytes()[0] & 0xF0) == 0xF0);
        }

        public ushort CountPlayers()
        {
            return (ushort)connections.Count;
        }

        public float GetPing(string nickname)
        {
            if (connections.TryGetValue(nickname, out var conn))
            {
                return conn.AvgRTT * 1000f; // ms
            }
            return 0.0f;
        }

        public void Dispose()
        {
            FinishAllConnections();
            DoubleSocket?.Dispose();
            Relay?.Dispose();
            KeyRSA?.Dispose();
        }

        // === THREAD SAFE SHUFFLER ===

        private static readonly Random _rng = new();
        private static readonly object _lock = new();
        private static void Shuffle(string[] array)
        {
            lock (_lock)
            {
                int n = array.Length;
                while (n > 1)
                {
                    int k = _rng.Next(n--);
                    string temp = array[n];
                    array[n] = array[k];
                    array[k] = temp;
                }
            }
        }
    }
}
