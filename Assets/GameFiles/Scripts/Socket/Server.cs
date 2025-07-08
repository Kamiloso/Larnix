using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
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

        private const float RSA_RESET_TIME = 1f; // seconds
        private float timeToResetRsa = RSA_RESET_TIME;
        private Dictionary<IPAddress, uint> recentRsaCount = new Dictionary<IPAddress, uint>();

        private const float NCN_RESET_TIME = 1f; // seconds
        private float timeToResetNcn = NCN_RESET_TIME;
        private Dictionary<IPAddress, uint> recentNcnCount = new Dictionary<IPAddress, uint>();

        private readonly Func<string, string, bool> TryLogin;
        private readonly Func<Packet, Packet> GetNcnAnswer;

        public Server(
            ushort port,
            ushort max_clients,
            bool allowInternetTraffic,
            RSA keyRSA,
            Func<string, string, bool> tryLogin,
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
                        else
                        {
                            UnityEngine.Debug.Log("Created double socket on random dynamic port.");
                            break;
                        }
                    }
                }
                else UnityEngine.Debug.Log("Created double socket on system-given dynamic port.");
            }
            else
            {
                if (!CreateDoubleSocket(port, allowInternetTraffic))
                {
                    ResetDoubleSocket();
                    throw new Exception("Couldn't create double socket on port " + port);
                }
                else UnityEngine.Debug.Log("Created double socket on set port.");
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
#if WINDOWS
            udpClientV4.Client.IOControl(
                (IOControlCode)(-1744830452),  // SIO_UDP_CONNRESET
                new byte[] { 0, 0, 0, 0 },     // false
                null
            );

            udpClientV6.Client.IOControl(
                (IOControlCode)(-1744830452),  // SIO_UDP_CONNRESET
                new byte[] { 0, 0, 0, 0 },     // false
                null
            );
#endif
            udpClientV4.Client.Blocking = false;
            udpClientV6.Client.Blocking = false;

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
                    if (ex.SocketErrorCode == SocketError.WouldBlock)
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
                    IPAddress ip = remoteEP.Address;
                    uint rsaCount = recentRsaCount.ContainsKey(ip) ? recentRsaCount[ip] : 0;
                    uint ncnCount = recentNcnCount.ContainsKey(ip) ? recentNcnCount[ip] : 0;

                    if (header.HasFlag(SafePacket.PacketFlag.RSA)) // encrypted with RSA
                    {
                        // This operation is expensive, so there are some limits for clients.
                        if (rsaCount < 3 && KeyRSA != null)
                        {
                            recentRsaCount[ip] = ++rsaCount;

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
                        // There are some limits for players to not flood the server
                        if(ncnCount < 3)
                        {
                            recentNcnCount[ip] = ++ncnCount;

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

                            if(IsIPv4Client)
                                udpClientV4.SendAsync(sendBytes, sendBytes.Length, remoteEP);
                            else
                                udpClientV6.SendAsync(sendBytes, sendBytes.Length, remoteEP);
                        }
                    }
                    else
                    {
                        if (header.HasFlag(SafePacket.PacketFlag.SYN)) // start connection
                        {
                            bool foundSessionConflict = false;
                            int foundFreeSpace = -1;
                            for (int i = 0; i < MaxClients; i++)
                            {
                                Connection conn = connections[i];

                                if (conn != null && conn.EndPoint.Equals(remoteEP))
                                {
                                    foundSessionConflict = true;
                                    break;
                                }

                                if (conn == null && foundFreeSpace == -1)
                                    foundFreeSpace = i;
                            }
                            if (foundSessionConflict || foundFreeSpace == -1)
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

                            if (nicknames.Contains(nickname) || !TryLogin(nickname, password))
                                continue;

                            Connection connection = new Connection(IsIPv4Client ? udpClientV4 : udpClientV6, remoteEP, keyAES);
                            connection.PushFromWeb(bytes, true);

                            connections[foundFreeSpace] = connection;
                            nicknames[foundFreeSpace] = nickname;
                        }
                        else // receive connection packet
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
                    Commands.Stop cmdStop = new Commands.Stop(0);
                    Packet packet = cmdStop.GetPacket();
                    packetList.Enqueue(new PacketAndOwner(nicknames[i], packet));

                    // reset player slots
                    connections[i] = null;
                    nicknames[i] = null;
                }
            }

            // Reset RSA counter
            timeToResetRsa -= deltaTime;
            if(timeToResetRsa < 0)
            {
                timeToResetRsa = RSA_RESET_TIME;
                recentRsaCount.Clear();
            }

            // Reset NCN counter
            timeToResetNcn -= deltaTime;
            if (timeToResetNcn < 0)
            {
                timeToResetNcn = NCN_RESET_TIME;
                recentNcnCount.Clear();
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

        public ushort CountPlayers() => (ushort)nicknames.Count(n => n != null);

        public void KillConnection(string nickname)
        {
            for (int i = 0; i < MaxClients; i++)
            {
                Connection connection = connections[i];
                if (nicknames[i] == nickname)
                {
                    connection.KillConnection();
                    break;
                }
            }
        }

        public void KillAllConnections()
        {
            foreach(Connection connection in connections)
            {
                if (connection != null)
                    connection.KillConnection();
            }
        }
        
        public void Dispose()
        {
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
