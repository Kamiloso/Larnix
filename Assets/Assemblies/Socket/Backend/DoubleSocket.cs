using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Socket.Channel;

namespace Socket.Backend
{
    internal class DoubleSocket : IDisposable
    {
        internal readonly ushort Port;
        internal UdpClient2 UdpClientV4;
        internal UdpClient2 UdpClientV6;

        private readonly System.Random _rand = new System.Random();

        internal DoubleSocket(ushort port, bool isLoopback)
        {
            if (port == 0)
            {
                if (!ConfigureSocket(0, isLoopback))
                {
                    int triesLeft = 8;

                    while (true)
                    {
                        if (triesLeft == 0)
                            throw new Exception("Couldn't create double socket on multiple random dynamic ports.");

                        port = (ushort)_rand.Next(49152, 65536);
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

            Port = UdpClientV4.Port; // V6 is the same
        }

        private bool ConfigureSocket(ushort port, bool isLoopback)
        {
            try
            {
                UdpClientV4 = new UdpClient2(
                    port: port,
                    isListener: true,
                    isLoopback: isLoopback,
                    isIPv6: false,
                    recvBufferSize: 1024 * 1024
                    );

                port = UdpClientV4.Port;

                UdpClientV6 = new UdpClient2(
                    port: port,
                    isListener: true,
                    isLoopback: isLoopback,
                    isIPv6: true,
                    recvBufferSize: 1024 * 1024
                    );
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    return false;

                throw;
            }

            return true;
        }

        internal bool TryReceive(out (IPEndPoint endPoint, byte[] data) result)
        {
            if (UdpClientV4.TryReceive(out var _resultV4))
            {
                result = _resultV4;
                return true;
            }

            if (UdpClientV6.TryReceive(out var _resultV6))
            {
                result = _resultV6;
                return true;
            }

            result = default;
            return false;
        }

        internal void Send(IPEndPoint endPoint, byte[] bytes)
        {
            bool IsIPv4Client = endPoint.AddressFamily == AddressFamily.InterNetwork;
            (IsIPv4Client ? UdpClientV4 : UdpClientV6).Send(endPoint, bytes);
        }

        public void Dispose()
        {
            UdpClientV4?.Dispose();
            UdpClientV6?.Dispose();
        }
    }
}
