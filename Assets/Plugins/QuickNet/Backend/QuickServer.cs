using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using QuickNet.Commands;
using System.Security.Cryptography;
using QuickNet.Processing;
using System;
using QuickNet.Channel;
using QuickNet.Data;

namespace QuickNet.Backend
{
    public class QuickServer : IDisposable
    {
        public const string LoopbackOnlyPassword = "SGP_PASSWORD\x01";

        public readonly ushort Port;
        public readonly ushort MaxClients;
        public readonly bool IsLoopback;
        public readonly string DataPath;
        public readonly long Secret;
        public readonly string Authcode;
        public readonly uint GameVersion;
        public readonly string UserText1;
        public readonly string UserText2;
        public readonly string UserText3;

        private readonly DoubleSocket DoubleSocket;
        private readonly ManagerNCN FastMessages;
        public readonly UserManager UserManager;
        public readonly RSA KeyRSA;
        public readonly HashSet<string> ReservedNicknames = new();

        private readonly string[] nicknames;
        private readonly Connection[] connections;

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
            if (!Validation.IsGoodUserText(userText1) || !Validation.IsGoodUserText(userText2) || !Validation.IsGoodUserText(userText3))
                throw new ArgumentException("Wrong UserText format! Cannot be larger than 128 characters or end with NULL (0x00).");

            // connection slots
            nicknames = new string[maxClients];
            connections = new Connection[maxClients];

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

        public void ReserveNickname(string nickname)
        {
            ReservedNicknames.Add(nickname);
        }

        private int FindFreeUserSlot(IPEndPoint endPoint)
        {
            foreach(Connection conn in connections)
            {
                if (conn != null && conn.EndPoint.Equals(endPoint))
                    return -1; // collision
            }

            for (int i = 0; i < MaxClients; i++)
            {
                Connection conn = connections[i];

                if (conn == null)
                    return i; // found
            }

            return -1; // no free slot
        }

        public void LoginAccept(IPEndPoint remoteEP)
        {
            if (!PreLoginBuffers.ContainsKey(remoteEP))
                throw new InvalidOperationException("Couldn't find login request to accept.");

            PreLoginBuffer preLoginBuffer = PreLoginBuffers[remoteEP];
            AllowConnection allowConnection = preLoginBuffer.AllowConnection;

            int freeSpace = FindFreeUserSlot(remoteEP);
            if (freeSpace == -1 || nicknames.Contains(allowConnection.Nickname))
            {
                LoginDeny(remoteEP);
                return;
            }

            bool IsIPv4Client = (remoteEP.AddressFamily == AddressFamily.InterNetwork);
            Connection connection = new Connection(
                IsIPv4Client ? DoubleSocket.UdpClientV4 : DoubleSocket.UdpClientV6, remoteEP, allowConnection.KeyAES);

            bool hasSyn = true;
            foreach (byte[] bytes in preLoginBuffer.GetBuffer())
            {
                connection.PushFromWeb(bytes, hasSyn);
                hasSyn = false;
            }

            connections[freeSpace] = connection;
            nicknames[freeSpace] = allowConnection.Nickname;

            PreLoginBuffers.Remove(remoteEP);
        }

        public void LoginDeny(IPEndPoint remoteEP)
        {
            if (!PreLoginBuffers.ContainsKey(remoteEP))
                throw new InvalidOperationException("Couldn't find login request to deny.");

            PreLoginBuffers.Remove(remoteEP);
        }

        public void ServerTick(float deltaTime)
        {
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

                SafePacket header = new SafePacket();
                if (header.TryDeserialize(bytes, true))
                {
                    InternetID internetID = new InternetID(remoteEP.Address);
                    uint conCount = recentConCount.ContainsKey(internetID) ? recentConCount[internetID] : 0;

                    // Limit packets with specific flags
                    if (header.HasFlag(SafePacket.PacketFlag.SYN) ||
                        header.HasFlag(SafePacket.PacketFlag.RSA) ||
                        header.HasFlag(SafePacket.PacketFlag.NCN))
                    {
                        if (conCount < MAX_CON) recentConCount[internetID] = ++conCount;
                        else continue;

                        if (globalConCount < MAX_GLOBAL_CON) globalConCount++;
                        else continue;
                    }

                    if (header.HasFlag(SafePacket.PacketFlag.RSA)) // encrypted with RSA
                    {
                        if (KeyRSA != null)
                        {
                            // Deserialize with decryption and serialize without encryption
                            SafePacket middlePacket = new SafePacket();
                            middlePacket.Encrypt = new Encryption.Settings(Encryption.Settings.Type.RSA, KeyRSA);
                            if (!middlePacket.TryDeserialize(bytes))
                                continue;
                            middlePacket.Encrypt = null;
                            bytes = middlePacket.Serialize();
                        }
                        else continue;
                    }

                    if(header.HasFlag(SafePacket.PacketFlag.NCN)) // fast question, fast answer
                    {
                        // Check packet type and answer properly
                        SafePacket ncnPacket = new SafePacket();
                        if (!ncnPacket.TryDeserialize(bytes))
                            continue;

                        FastMessages.ProcessNCN(remoteEP, ncnPacket.SeqNum, ncnPacket.Payload);
                    }
                    else
                    {
                        if (header.HasFlag(SafePacket.PacketFlag.SYN)) // start connection
                        {
                            if (FindFreeUserSlot(remoteEP) == -1)
                                continue;

                            SafePacket safeSynPacket = new SafePacket();
                            if (!safeSynPacket.TryDeserialize(bytes))
                                continue;

                            Packet synPacket = safeSynPacket.Payload;
                            if (synPacket == null)
                                continue;

                            if (synPacket.ID != CmdID.AllowConnection)
                                continue;

                            Commands.AllowConnection allowConnection = new Commands.AllowConnection(synPacket);
                            if (allowConnection.HasProblems)
                                continue;

                            string nickname = allowConnection.Nickname;
                            string password = allowConnection.Password;
                            byte[] keyAES = allowConnection.KeyAES;
                            long serverSecret = allowConnection.ServerSecret;
                            long challengeID = allowConnection.ChallengeID;
                            long timestamp = allowConnection.Timestamp;

                            if (nicknames.Contains(nickname))
                                continue;

                            if (!PreLoginBuffers.ContainsKey(remoteEP))
                            {
                                PreLoginBuffer preLoginBuffer = new PreLoginBuffer(allowConnection);
                                preLoginBuffer.AddPacket(bytes);
                                PreLoginBuffers.Add(remoteEP, preLoginBuffer);

                                FastMessages.TryLogin(remoteEP, nickname, password, serverSecret, challengeID, timestamp);
                            }
                        }
                        else // receive connection packet
                        {
                            if(PreLoginBuffers.ContainsKey(remoteEP))
                            {
                                PreLoginBuffers[remoteEP].AddPacket(bytes);
                            }
                            else
                            {
                                foreach (Connection conn in connections)
                                {
                                    if (conn != null && conn.EndPoint.Equals(remoteEP))
                                    {
                                        conn.PushFromWeb(bytes);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Tick every connection
            foreach (Connection connection in connections)
            {
                if (connection != null)
                    connection.Tick(deltaTime);
            }

            // Receive packets, randomize client order
            Queue<(Packet, string)> packetList = new();
            int[] clientIDs = Enumerable.Range(0, MaxClients).ToArray();
            Shuffle(clientIDs);
            foreach (int clientID in clientIDs)
            {
                Connection conn = connections[clientID];
                string nickname = nicknames[clientID];

                if (conn != null)
                {
                    Queue<Packet> packets = conn.Receive();
                    while (packets.Count > 0)
                        packetList.Enqueue((packets.Dequeue(), nickname));
                }
            }

            // Remove dead connections
            for (int i = 0; i < MaxClients; i++)
            {
                Connection conn = connections[i];
                if (conn != null && conn.IsDead)
                {
                    // add finishing message
                    Commands.Stop cmdStop = new Commands.Stop();
                    Packet packet = cmdStop.GetPacket();
                    packetList.Enqueue((packet, nicknames[i]));

                    // reset player slots
                    connections[i] = null;
                    nicknames[i] = null;
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

                if (Subscriptions.TryGetValue(packet.ID, out var Execute))
                {
                    Execute(packet, owner);
                }
            }
        }

        public void Subscribe<T>(Action<T, string> InterpretPacket) where T : BaseCommand
        {
            CmdID ID = BaseCommand.GetCommandID(typeof(T));
            Subscriptions[ID] = (Packet packet, string owner) =>
            {
                T command = BaseCommand.CreateGeneric<T>(packet);
                if (command != null && !command.HasProblems)
                {
                    InterpretPacket(command, owner);
                }
            };
        }

        public void Send(string nickname, Packet packet, bool safemode = true)
        {
            for (int i = 0; i < MaxClients; i++)
            {
                Connection connection = connections[i];
                if (nicknames[i] == nickname)
                {
                    connection.Send(packet, safemode);
                    break;
                }
            }
        }

        public void Broadcast(Packet packet, bool safemode = true)
        {
            foreach (var connection in connections)
            {
                if (connection != null)
                    connection.Send(packet, safemode);
            }
        }

        public void SendNCN(IPEndPoint endPoint, uint ncnID, Packet packet)
        {
            SafePacket safeAnswer = new SafePacket(
                ncnID,
                0,
                (byte)SafePacket.PacketFlag.NCN,
                packet
                );

            byte[] sendBytes = safeAnswer.Serialize();
            DoubleSocket.Send(sendBytes, endPoint);
        }

        public IPEndPoint GetClientEndPoint(string nickname)
        {
            for (int i = 0; i < MaxClients; i++)
            {
                Connection connection = connections[i];
                if (nicknames[i] == nickname)
                    return connection.EndPoint;
            }
            return null;
        }

        public ushort CountPlayers()
        {
            return (ushort)nicknames.Count(n => n != null);
        }

        public void FinishConnection(string nickname)
        {
            for (int i = 0; i < MaxClients; i++)
            {
                Connection connection = connections[i];
                if (nicknames[i] == nickname)
                {
                    connection.FinishConnection();
                    break;
                }
            }
        }

        public void FinishAllConnections()
        {
            foreach(Connection connection in connections)
            {
                if (connection != null)
                    connection.FinishConnection();
            }
        }

        public float GetPing(string nickname)
        {
            for (int i = 0; i < MaxClients; i++)
            {
                Connection connection = connections[i];
                if (nicknames[i] == nickname)
                {
                    return connection.AvgRTT * 1000f; // ms
                }
            }
            return 0.0f;
        }

        public void Dispose()
        {
            FinishAllConnections();
            DoubleSocket?.Dispose();
            KeyRSA?.Dispose();
        }

        private static void Shuffle(int[] array)
        {
            System.Random rng = new System.Random();
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                int temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }
    }
}
