using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System;

namespace Larnix.Socket
{
    public class Client : IDisposable
    {
        public string Nickname { get; private set; } = "";
        private UdpClient udpClient = null;
        private Connection connection = null;
        private IPEndPoint remoteEndPoint = null;

        public Client(IPEndPoint endPoint, string nickname, string password, RSA keyPublicRSA = null)
        {
            Nickname = nickname;
            remoteEndPoint = endPoint;

            udpClient = CreateConfiguredClientObject(endPoint);

            byte[] keyAES = new byte[16];
            using(var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyAES);
            }

            Commands.AllowConnection allowConnection = new Commands.AllowConnection(
                Nickname,
                password,
                keyAES
                );
            if (allowConnection.HasProblems)
                throw new System.Exception("Couldn't construct AllowConnection command.");

            connection = new Connection(udpClient, endPoint, keyAES, allowConnection.GetPacket(), keyPublicRSA);
        }

        public static UdpClient CreateConfiguredClientObject(IPEndPoint endPoint)
        {
            IPAddress ad = endPoint.Address;
            AddressFamily af = endPoint.AddressFamily;

            UdpClient udpClient = new UdpClient(af);

            if (af == AddressFamily.InterNetwork)
            {
                if (IPAddress.IsLoopback(ad))
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                else
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            }
            else if (af == AddressFamily.InterNetworkV6)
            {
                if (IPAddress.IsLoopback(ad))
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
                else
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
            }
            else throw new System.NotSupportedException("Unknown address type.");

            udpClient.Client.Blocking = false;
            return udpClient;
        }

        public Queue<Packet> ClientTickAndReceive(float deltaTime)
        {
            // Get packets
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
                        continue;
                    else
                        throw;
                }

                if (bytes == null)
                    continue;

                if(remoteEndPoint.Equals(remoteEP))
                    connection.PushFromWeb(bytes);
            }

            connection.Tick(deltaTime);
            return connection.Receive();
        }

        public void Send(Packet packet, bool safemode = true)
        {
            connection.Send(packet, safemode);
        }

        public void KillConnection()
        {
            connection.KillConnection();
        }

        public bool IsDead()
        {
            return connection.IsDead;
        }

        public void Dispose()
        {
            udpClient.Dispose();
        }
    }
}
