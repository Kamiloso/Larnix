using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Larnix.Socket.Packets;
using System.Collections.Generic;
using Larnix.Core.Utils;

namespace Larnix.Socket.Channel.Networking
{
    internal class TripleSocket : INetworkInteractions, IDisposable
    {
        public const ushort PREF_DYNAMIC_PORT = 50_000;
        private const int EP_CACHE_CAPACITY = 1 << 16; // over 65k

        public ushort Port { get; }
        private UdpClient2 _udp4;
        private UdpClient2 _udp6;

        private readonly HashSet<IPEndPoint> _relayEndPoints = new();

        private volatile RelayConnection _relayUdpClient;
        private volatile int _relayEnabled = 0;
        
        private readonly object _lock = new();
        private volatile bool _disposed;

        public TripleSocket(ushort port, bool isLoopback)
        {
            if (port == 0)
            {
                if (!ConfigureSocket(PREF_DYNAMIC_PORT, isLoopback))
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
            }
            else
            {
                if (!ConfigureSocket(port, isLoopback))
                {
                    throw new Exception("Couldn't create double socket on port " + port);
                }
            }

            Port = _udp4.Port; // V6 is the same
        }

        private bool ConfigureSocket(ushort port, bool isLoopback)
        {
            try
            {
                _udp4 = new UdpClient2(
                    port: port,
                    isListener: true,
                    isLoopback: isLoopback,
                    isIPv6: false,
                    recvBufferSize: 1024 * 1024
                    );

                port = _udp4.Port;

                _udp6 = new UdpClient2(
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

        public async Task<ushort?> StartRelayAsync(string address)
        {
            if (Interlocked.CompareExchange(ref _relayEnabled, 1, 0) == 0)
            {
                RelayConnection relayCon = await RelayConnection.EstablishRelayAsync(address);

                lock (_lock)
                {
                    if (_disposed)
                    {
                        relayCon?.Dispose();
                        return null; // failed
                    }
                    else
                    {
                        _relayUdpClient = relayCon;
                        return _relayUdpClient?.RemotePort; // null or port
                    }
                }
            }

            return null; // second activation returns null
        }

        public void KeepAlive()
        {
            _relayUdpClient?.KeepAlive();
        }

        public bool TryReceive(out DataBox result)
        {
            if (_udp4.TryReceive(out var _resultV4))
            {
                result = _resultV4;
                _relayEndPoints.Remove(_resultV4.target);
                return true;
            }

            if (_udp6.TryReceive(out var _resultV6))
            {
                result = _resultV6;
                return true;
            }

            if (_relayUdpClient?.TryReceive(out var _resultR) == true)
            {
                result = _resultR;
                if (_relayEndPoints.Count < EP_CACHE_CAPACITY)
                    _relayEndPoints.Add(_resultR.target);
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
                if (_relayEndPoints.Contains(remoteEP))
                    _relayUdpClient?.Send(remoteEP, bytes); // relay IPv4 send
                else
                    _udp4.Send(remoteEP, bytes); // direct IPv4 send
            }
            else
            {
                _udp6.Send(remoteEP, bytes); // IPv6 send
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    _disposed = true;

                    _udp4?.Dispose();
                    _udp6?.Dispose();
                    _relayUdpClient?.Dispose();
                }
            }
        }
    }
}
