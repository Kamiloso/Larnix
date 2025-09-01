using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using QuickNet.Processing;
using QuickNet.Channel;
using QuickNet.Channel.Cmds;
using System.Threading.Tasks;
using System;

namespace QuickNet.Backend
{
    public class QuickServer : IDisposable
    {
        public const string LoopbackOnlyPassword = "SGP_PASSWORD\x01";

        public readonly ushort Port;
        public readonly ushort MaxClients;
        public readonly bool IsLoopback;
        public readonly string DataPath;
        public readonly uint GameVersion;
        public readonly string UserText1;
        public readonly string UserText2;
        public readonly string UserText3;

        public readonly long Secret;
        public readonly string Authcode;

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

        public QuickServer(ushort port, ushort maxClients, bool isLoopback, string dataPath, uint gameVersion,
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
            UserManager = new UserManager(this, dataPath);
            KeyRSA = KeyObtainer.ObtainKeyRSA(dataPath);

            // other configuration
            Port = DoubleSocket.Port;
            MaxClients = maxClients;
            IsLoopback = isLoopback;
            DataPath = dataPath;
            Secret = KeyObtainer.ObtainSecret(dataPath);
            Authcode = Processing.Authcode.ProduceAuthCodeRSA(KeyObtainer.KeyToPublicBytes(KeyRSA), Secret);
            GameVersion = gameVersion;
            UserText1 = userText1;
            UserText2 = userText2;
            UserText3 = userText3;
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

        public void ReserveNickname(string nickname)
        {
            ReservedNicknames.Add(nickname);
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

            bool IsIPv4Client = (remoteEP.AddressFamily == AddressFamily.InterNetwork);
            Connection connection = new Connection(
                IsIPv4Client ? DoubleSocket.UdpClientV4 : DoubleSocket.UdpClientV6, remoteEP, allowConnection.KeyAES);

            RememberConnection(nickname, connection);
            PreLoginBuffers.Remove(remoteEP);

            bool hasSyn = true;
            foreach (byte[] bytes in preLoginBuffer.GetBuffer())
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

            // Get and divide packets between clients
            while (DoubleSocket.DataAvailable())
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = null;

                try
                {
                    bytes = DoubleSocket.Receive(ref remoteEP);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock || ex.SocketErrorCode == SocketError.ConnectionReset)
                        continue;
                    else
                        throw;
                }

                if (bytes == null)
                    continue;

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
                        else continue;

                        if (globalConCount < MAX_GLOBAL_CON) globalConCount++;
                        else continue;
                    }

                    if (header.HasFlag(PacketFlag.RSA)) // encrypted with RSA
                    {
                        if (KeyRSA != null)
                        {
                            // Deserialize with decryption and serialize without encryption
                            QuickPacket middlePacket = new QuickPacket();
                            middlePacket.Encrypt = new Encryption.Settings(Encryption.Settings.Type.RSA, KeyRSA);
                            if (!middlePacket.TryDeserialize(bytes))
                                continue;

                            middlePacket.Encrypt = null;
                            bytes = middlePacket.Serialize();
                        }
                        else continue;
                    }

                    if(header.HasFlag(PacketFlag.NCN)) // fast question, fast answer
                    {
                        // Check packet type and answer properly
                        QuickPacket ncnPacket = new QuickPacket();
                        if (!ncnPacket.TryDeserialize(bytes))
                            continue;

                        FastMessages.ProcessNCN(remoteEP, ncnPacket.SeqNum, ncnPacket.Packet);
                    }
                    else
                    {
                        if (header.HasFlag(PacketFlag.SYN)) // start connection
                        {
                            QuickPacket safeSynPacket = new QuickPacket();
                            if (!safeSynPacket.TryDeserialize(bytes))
                                continue;

                            if (Payload.TryConstructPayload<AllowConnection>(safeSynPacket.Packet, out var allowcon))
                            {
                                string nickname = allowcon.Nickname;
                                string password = allowcon.Password;
                                long serverSecret = allowcon.ServerSecret;
                                long challengeID = allowcon.ChallengeID;
                                long timestamp = allowcon.Timestamp;

                                if (!CanAcceptSYN(remoteEP, nickname))
                                    continue;

                                if (!PreLoginBuffers.ContainsKey(remoteEP))
                                {
                                    PreLoginBuffer preLoginBuffer = new PreLoginBuffer(allowcon);
                                    preLoginBuffer.AddPacket(bytes);
                                    PreLoginBuffers.Add(remoteEP, preLoginBuffer);

                                    FastMessages.TryLogin(remoteEP, nickname, password, serverSecret, challengeID, timestamp);
                                }
                            }
                            else continue;
                        }
                        else // receive connection packet
                        {
                            if(PreLoginBuffers.TryGetValue(remoteEP, out var preBuffer))
                            {
                                preBuffer.AddPacket(bytes);
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

        internal void SendNCN(IPEndPoint endPoint, uint ncnID, Packet packet)
        {
            QuickPacket safeAnswer = new QuickPacket(
                ncnID,
                0,
                (byte)PacketFlag.NCN,
                packet
                );

            byte[] sendBytes = safeAnswer.Serialize();
            DoubleSocket.Send(sendBytes, endPoint);
        }

        public IPEndPoint GetClientEndPoint(string nickname)
        {
            if (connections.TryGetValue(nickname, out var conn))
                return conn.EndPoint;
            return null;
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
            KeyRSA?.Dispose();
        }

        // === THREAD SAFE SHUFFLER ===

        private static readonly Random _rng = new();
        private static readonly object _locker = new();
        private static void Shuffle(string[] array)
        {
            lock (_locker)
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
