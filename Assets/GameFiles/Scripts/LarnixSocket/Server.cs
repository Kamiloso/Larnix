using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using Larnix.Socket.Commands;
using Unity.VisualScripting;

namespace Larnix.Socket
{
    public class Server
    {
        public ushort Port { get; private set; }
        public int MaxClients { get; private set; }

        private UdpClient udpClient = null;
        private string[] nicknames = null;
        private Connection[] connections = null;

        public Server(ushort port = 0, int max_clients = 12)
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
                    if (ex.SocketErrorCode == SocketError.WouldBlock)
                        break;
                    else
                        throw;
                }

                SafePacket header = new SafePacket();
                if (header.TryDeserialize(bytes, true))
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
                        // ...
                        // ...
                        // Here you can add nickname validation and AES initialization
                        // ...
                        // ...

                        Connection connection = new Connection(udpClient, remoteEP, null);
                        connection.PushFromWeb(bytes);

                        connections[foundFreeSpace] = connection;
                        nicknames[foundFreeSpace] = nickname;
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
        
        public void DisposeUdp()
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
