using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Larnix.Socket.Structs;
using System.Collections.Generic;

namespace Larnix.Socket.Channel
{
    internal class TripleSocket : INetworkInteractions, IDisposable
    {
        public readonly ushort Port;
        private UdpClient2 UdpClient4;
        private UdpClient2 UdpClient6;

        private HashSet<IPEndPoint> RelayEndPoints = new();
        private const int EP_CACHE_CAPACITY = 1 << 16; // over 65k

        private volatile RelayConnection RelayUdpClient;
        private volatile int _relayEnabled = 0;
        private volatile bool _disposeStarted = false;
        
        private bool _disposed;

        public TripleSocket(ushort port, bool isLoopback)
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

        public async Task<ushort?> StartRelay(string address)
        {
            if (Interlocked.CompareExchange(ref _relayEnabled, 1, 0) == 0)
            {
                RelayUdpClient = await RelayConnection.EstablishRelayAsync(address);

                if (_disposeStarted)
                {
                    RelayUdpClient.Dispose();
                    return null; // failed
                }

                return RelayUdpClient?.RemotePort; // null or port
            }

            return null; // second activation returns null
        }

        public void KeepAlive()
        {
            RelayUdpClient?.KeepAlive();
        }

        public bool TryReceive(out DataBox result)
        {
            if (UdpClient4.TryReceive(out var _resultV4))
            {
                result = _resultV4;
                RelayEndPoints.Remove(_resultV4.target);
                return true;
            }

            if (UdpClient6.TryReceive(out var _resultV6))
            {
                result = _resultV6;
                return true;
            }

            if (RelayUdpClient?.TryReceive(out var _resultR) == true)
            {
                result = _resultR;
                if (RelayEndPoints.Count < EP_CACHE_CAPACITY)
                    RelayEndPoints.Add(_resultR.target);
                return true;
            }

            result = null;
            return false;
        }

        public void Send(IPEndPoint remoteEP, byte[] bytes)
        {
            bool isIPv4Client = remoteEP.AddressFamily == AddressFamily.InterNetwork;
            if (isIPv4Client)
            {
                if (RelayEndPoints.Contains(remoteEP))
                    RelayUdpClient?.Send(remoteEP, bytes); // relay IPv4 send
                else
                    UdpClient4.Send(remoteEP, bytes); // direct IPv4 send
            }
            else
            {
                UdpClient6.Send(remoteEP, bytes); // IPv6 send
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _disposeStarted = true;

                UdpClient4?.Dispose();
                UdpClient6?.Dispose();
                RelayUdpClient?.Dispose();
            }
        }
    }
}
