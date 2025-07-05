using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using Larnix.Socket.Commands;
using Unity.VisualScripting;
using System.Security.Cryptography;

namespace Larnix.Socket
{
    public class Server
    {
        public ushort Port { get; private set; }
        public int MaxClients { get; private set; }
        private RSA KeyRSA = null;

        private UdpClient udpClient = null;
        private string[] nicknames = null;
        private Connection[] connections = null;

        private const float RSA_RESET_TIME = 1f; // seconds
        private float timeToResetRsa = RSA_RESET_TIME;
        private Dictionary<IPAddress, uint> recentRsaCount = new Dictionary<IPAddress, uint>();

        public Server(ushort port, int max_clients, RSA keyRSA)
        {
            udpClient = new UdpClient(AddressFamily.InterNetworkV6);
            udpClient.Client.SetSocketOption(
                SocketOptionLevel.IPv6,
                SocketOptionName.IPv6Only,
                false
            );
            udpClient.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            udpClient.Client.Blocking = false;

            Port = (ushort)((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
            MaxClients = max_clients;

            nicknames = new string[max_clients];
            connections = new Connection[max_clients];
            KeyRSA = keyRSA;
        }

        public Queue<PacketAndOwner> ServerTickAndReceive(float deltaTime)
        {
            // Get and divide packets between clients
            while (udpClient.Available > 0)
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = null;

                try
                {
                    bytes = udpClient.Receive(ref remoteEP);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock || ex.SocketErrorCode == SocketError.ConnectionReset)
                        break;
                    else
                        throw;
                }

                SafePacket header = new SafePacket();
                if (header.TryDeserialize(bytes, true))
                {
                    if(header.HasFlag(SafePacket.PacketFlag.RSA)) // encrypted with RSA
                    {
                        IPAddress ip = remoteEP.Address;
                        uint rsaCount = recentRsaCount.ContainsKey(ip) ? recentRsaCount[ip] : 0;

                        // This operation is expensive, so there are some limits for clients.
                        if (rsaCount < 3)
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

                            if (nicknames.Contains(nickname))
                                continue;

                            // ...
                            // ...
                            // Here you can add nickname validation and AES initialization
                            // ...
                            // ...

                            Connection connection = new Connection(udpClient, remoteEP, keyAES);
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
            while(timeToResetRsa < 0)
            {
                timeToResetRsa += RSA_RESET_TIME;
                recentRsaCount.Clear();
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
            udpClient.Dispose();
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
