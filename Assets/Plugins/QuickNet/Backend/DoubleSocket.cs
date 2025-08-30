using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace QuickNet.Backend
{
    public class DoubleSocket : IDisposable
    {
        public readonly ushort Port;
        public UdpClient UdpClientV4;
        public UdpClient UdpClientV6;

        public DoubleSocket(ushort port, bool isLoopback)
        {
            if (port == 0)
            {
                if (!ConfigureSocket(0, isLoopback))
                {
                    System.Random rand = new System.Random();
                    int triesLeft = 8;

                    while (true)
                    {
                        if (triesLeft == 0)
                            throw new Exception("Couldn't create double socket on multiple random dynamic ports.");

                        port = (ushort)rand.Next(49152, 65536);
                        if (!ConfigureSocket(port, isLoopback))
                        {
                            triesLeft--;
                        }
                        else break;
                    }
                }
            }
            else
            {
                if (!ConfigureSocket(port, isLoopback))
                {
                    throw new Exception("Couldn't create double socket on port " + port);
                }
            }

            Port = (ushort)((IPEndPoint)UdpClientV4.Client.LocalEndPoint).Port; // V6 is the same
        }

        private bool ConfigureSocket(ushort port, bool isLoopback)
        {
            UdpClientV4 = new UdpClient(AddressFamily.InterNetwork);
            UdpClientV6 = new UdpClient(AddressFamily.InterNetworkV6);

            IPAddress linkV4 = isLoopback ? IPAddress.Loopback : IPAddress.Any;
            IPAddress linkV6 = isLoopback ? IPAddress.IPv6Loopback : IPAddress.IPv6Any;

            try
            {
                UdpClientV4.Client.Bind(new IPEndPoint(linkV4, port));
                port = (ushort)((IPEndPoint)UdpClientV4.Client.LocalEndPoint).Port;
                UdpClientV6.Client.Bind(new IPEndPoint(linkV6, port));
            }
            catch
            {
                UdpClientV4?.Dispose();
                UdpClientV6?.Dispose();
                return false;
            }

            UdpClientV4.Client.Blocking = false;
            UdpClientV6.Client.Blocking = false;

            UdpClientV4.Client.ReceiveBufferSize = 1024 * 1024; // 1 MB
            UdpClientV6.Client.ReceiveBufferSize = 1024 * 1024; // 1 MB

            return true;
        }

        public bool DataAvailable()
        {
            return UdpClientV4.Available > 0 || UdpClientV6.Available > 0;
        }

        public byte[] Receive(ref IPEndPoint remoteEP)
        {
            if (UdpClientV4.Available > 0)
                return UdpClientV4.Receive(ref remoteEP);

            if (UdpClientV6.Available > 0)
                return UdpClientV6.Receive(ref remoteEP);

            return null;
        }

        public void Send(byte[] bytes, IPEndPoint endPoint)
        {
            bool IsIPv4Client = endPoint.AddressFamily == AddressFamily.InterNetwork;
            (IsIPv4Client ? UdpClientV4 : UdpClientV6).SendAsync(bytes, bytes.Length, endPoint);
        }

        public void Dispose()
        {
            UdpClientV4?.Dispose();
            UdpClientV6?.Dispose();
        }
    }
}
