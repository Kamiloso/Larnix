using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using Larnix.Socket.Commands;
using Unity.VisualScripting;
using System.Security.Cryptography;
using System;

namespace Larnix.Socket
{
    public class Server : IDisposable
    {
        public ushort Port { get; private set; }
        public ushort MaxClients { get; private set; }
        private RSA KeyRSA = null;

        private UdpClient udpClientV4 = null;
        private UdpClient udpClientV6 = null;
        private string[] nicknames = null;
        private Connection[] connections = null;

        private const float CON_RESET_TIME = 1f; // seconds
        private float timeToResetCon = CON_RESET_TIME;
        private Dictionary<InternetID, uint> recentConCount = new();

        private readonly Action<IPEndPoint, string, string> TryLogin;
        private readonly Func<Packet, Packet> GetNcnAnswer;

        private readonly Dictionary<IPEndPoint, PreLoginBuffer> PreLoginBuffers = new();

        public Server(
            ushort port,
            ushort max_clients,
            bool allowInternetTraffic,
            RSA keyRSA,
            Action<IPEndPoint, string, string> tryLogin,
            Func<Packet, Packet> getNcnAnswer)
        {
            TryLogin = tryLogin;
            GetNcnAnswer = getNcnAnswer;

            if (port == 0)
            {
                if (!CreateDoubleSocket(0, allowInternetTraffic))
                {
                    ResetDoubleSocket();

                    System.Random rand = new System.Random();
                    int triesLeft = 8;

                    while (true)
                    {
                        int try_port = rand.Next(49152, 65536);

                        if (triesLeft-- == 0)
                            throw new Exception("Couldn't create double socket on multiple random dynamic ports.");

                        if (!CreateDoubleSocket((ushort)try_port, allowInternetTraffic))
                        {
                            ResetDoubleSocket();
                        }
                        else break; // SUCCESS: random dynamic port
                    }
                }
                else { } // SUCCESS: system-given dynamic port
            }
            else
            {
                if (!CreateDoubleSocket(port, allowInternetTraffic))
                {
                    ResetDoubleSocket();
                    throw new Exception("Couldn't create double socket on port " + port);
                }
                else { } // SUCCESS: set port
            }

            MaxClients = max_clients;

            nicknames = new string[max_clients];
            connections = new Connection[max_clients];
            KeyRSA = keyRSA;
        }

        private bool CreateDoubleSocket(ushort port, bool allowInternetTraffic)
        {
            udpClientV4 = new UdpClient(AddressFamily.InterNetwork);
            udpClientV6 = new UdpClient(AddressFamily.InterNetworkV6);

            IPAddress linkV4 = allowInternetTraffic ? IPAddress.Any : IPAddress.Loopback;
            IPAddress linkV6 = allowInternetTraffic ? IPAddress.IPv6Any : IPAddress.IPv6Loopback;

            try
            {
                udpClientV4.Client.Bind(new IPEndPoint(linkV4, port));
                port = (ushort)((IPEndPoint)udpClientV4.Client.LocalEndPoint).Port;
                udpClientV6.Client.Bind(new IPEndPoint(linkV6, port));
            }
            catch
            {
                return false;
            }

            udpClientV4.Client.Blocking = false;
            udpClientV6.Client.Blocking = false;

            udpClientV4.Client.ReceiveBufferSize = 1024 * 1024; // 1 MB
            udpClientV6.Client.ReceiveBufferSize = 1024 * 1024; // 1 MB

            Port = port;
            return true;
        }

        private void ResetDoubleSocket()
        {
            if(udpClientV4 != null)
                udpClientV4.Dispose();

            if(udpClientV6 != null)
                udpClientV6.Dispose();

            udpClientV4 = null;
            udpClientV6 = null;
        }

        private byte[] DoubleSocketReceive(ref IPEndPoint remoteEP)
        {
            if(udpClientV4.Available > 0)
                return udpClientV4.Receive(ref remoteEP);

            if(udpClientV6.Available > 0)
                return udpClientV6.Receive(ref remoteEP);

            return null;
        }

        private int FindFreeUserSlot(IPEndPoint endPoint) // -1 => problem occured
        {
            foreach(Connection conn in connections)
            {
                if (conn != null && conn.EndPoint.Equals(endPoint))
                    return -1; // endpoint collision
            }

            for (int i = 0; i < MaxClients; i++)
            {
                Connection conn = connections[i];

                if (conn == null)
                    return i; // found
            }

            return -1; // no free slot found
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
            Connection connection = new Connection(IsIPv4Client ? udpClientV4 : udpClientV6, remoteEP, allowConnection.KeyAES);

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

        public Queue<PacketAndOwner> ServerTickAndReceive(float deltaTime)
        {
            // Get and divide packets between clients
            while (udpClientV4.Available > 0 || udpClientV6.Available > 0)
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = null;

                try
                {
                    bytes = DoubleSocketReceive(ref remoteEP);
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

                bool IsIPv4Client = (remoteEP.AddressFamily == AddressFamily.InterNetwork);

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
                        if (conCount < 3)
                            recentConCount[internetID] = ++conCount;
                        else
                            continue;
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

                        Packet answer = GetNcnAnswer(ncnPacket.Payload);
                        if (answer == null)
                            continue;

                        SafePacket safeAnswer = new SafePacket(
                            ncnPacket.SeqNum,
                            0,
                            (byte)SafePacket.PacketFlag.NCN,
                            answer
                            );

                        byte[] sendBytes = safeAnswer.Serialize();

                        if (IsIPv4Client)
                            udpClientV4.SendAsync(sendBytes, sendBytes.Length, remoteEP);
                        else
                            udpClientV6.SendAsync(sendBytes, sendBytes.Length, remoteEP);
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

                            if (synPacket.ID != (byte)Commands.Name.AllowConnection)
                                continue;

                            Commands.AllowConnection allowConnection = new Commands.AllowConnection(synPacket);
                            if (allowConnection.HasProblems)
                                continue;

                            string nickname = allowConnection.Nickname;
                            string password = allowConnection.Password;
                            byte[] keyAES = allowConnection.KeyAES;

                            if (nicknames.Contains(nickname))
                                continue;

                            if (!PreLoginBuffers.ContainsKey(remoteEP))
                            {
                                PreLoginBuffer preLoginBuffer = new PreLoginBuffer(allowConnection);
                                preLoginBuffer.AddPacket(bytes);
                                PreLoginBuffers.Add(remoteEP, preLoginBuffer);

                                TryLogin(remoteEP, nickname, password);
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
            Queue<PacketAndOwner> packetList = new Queue<PacketAndOwner>();
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
                        packetList.Enqueue(new PacketAndOwner(nickname, packets.Dequeue()));
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
                    packetList.Enqueue(new PacketAndOwner(nicknames[i], packet));

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
            }

            // Return packets
            return packetList;
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

        public ushort CountPlayers() => (ushort)nicknames.Count(n => n != null);

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
        
        public void Dispose()
        {
            FinishAllConnections();
            ResetDoubleSocket();
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
