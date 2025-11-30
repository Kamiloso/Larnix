using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Larnix.Socket.UdpClients;

namespace Larnix.Socket.Backend
{
    internal class DoubleSocket : IDisposable
    {
        public readonly ushort Port;
        public UdpClient2 UdpClient4;
        public UdpClient2 UdpClient6;

        private bool _disposed;

        public DoubleSocket(ushort port, bool isLoopback)
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

                        Random rand = new Random();
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

            Port = UdpClient4.Port; // V6 is the same
        }

        private bool ConfigureSocket(ushort port, bool isLoopback)
        {
            try
            {
                UdpClient4 = new UdpClient2(
                    port: port,
                    isListener: true,
                    isLoopback: isLoopback,
                    isIPv6: false,
                    recvBufferSize: 1024 * 1024
                    );

                port = UdpClient4.Port;

                UdpClient6 = new UdpClient2(
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

        public bool TryReceive(out (IPEndPoint endPoint, byte[] data) result)
        {
            if (UdpClient4.TryReceive(out var _resultV4))
            {
                result = _resultV4;
                return true;
            }

            if (UdpClient6.TryReceive(out var _resultV6))
            {
                result = _resultV6;
                return true;
            }

            result = default;
            return false;
        }

        public void Send(IPEndPoint endPoint, byte[] bytes)
        {
            bool IsIPv4Client = endPoint.AddressFamily == AddressFamily.InterNetwork;
            (IsIPv4Client ? UdpClient4 : UdpClient6).Send(endPoint, bytes);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                UdpClient4?.Dispose();
                UdpClient6?.Dispose();
            }
        }
    }
}
