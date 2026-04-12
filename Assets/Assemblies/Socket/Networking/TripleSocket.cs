#nullable enable
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using Larnix.Core.Utils;

namespace Larnix.Socket.Networking;

// This class is not thread-safe... but it doesn't need to be

internal class TripleSocket : INetworkInteractions, IDisposable
{
    private ushort PrefDynamicPort => 50_000;
    private int EpCacheCapacity => 65_536;

    public ushort LocalPort { get; }
    public Task<string?> RelayForeignAddressTask => _relay.ForeignAddressTask;

    private readonly UdpClient2 _udp4;
    private readonly UdpClient2 _udp6;
    private readonly RelayClient _relay;

    private readonly HashSet<IPEndPoint> _relayEndPoints = new();

    private bool _disposed;

    public TripleSocket(ushort port, bool isLoopback, string? relayAddress)
    {
        object[] sockets;

        if (port == 0)
        {
            if (!TryConfigureSocket(PrefDynamicPort, isLoopback, relayAddress, out sockets))
            {
                if (!TryConfigureSocket(0, isLoopback, relayAddress, out sockets))
                {
                    int triesLeft = 8;

                    while (true)
                    {
                        if (triesLeft == 0)
                        {
                            throw new InvalidOperationException("Couldn't create double udp socket on multiple random dynamic ports.");
                        }

                        port = (ushort)RandUtils.Rand.Next(49_152, 65_536);
                        if (!TryConfigureSocket(port, isLoopback, relayAddress, out sockets))
                        {
                            triesLeft--;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            if (!TryConfigureSocket(port, isLoopback, relayAddress, out sockets))
            {
                throw new InvalidOperationException($"Couldn't create double socket on port " + port);
            }
        }

        _udp4 = (UdpClient2)sockets[0];
        _udp6 = (UdpClient2)sockets[1];
        _relay = (RelayClient)sockets[2];

        LocalPort = _udp4.Port;
    }

    private static bool TryConfigureSocket(ushort port, bool isLoopback, string? relayAddress, out object[] sockets)
    {
        UdpClient2? udp4 = null;
        UdpClient2? udp6 = null;
        RelayClient? relay = null;

        try
        {
            udp4 = new UdpClient2(
                port: port,
                isListener: true,
                isLoopback: isLoopback,
                isIPv6: false,
                recvBufferSize: 1024 * 1024
                );

            udp6 = new UdpClient2(
                port: udp4.Port,
                isListener: true,
                isLoopback: isLoopback,
                isIPv6: true,
                recvBufferSize: 1024 * 1024
                );

            relay = new RelayClient(relayAddress);

            sockets = new object[] { udp4, udp6, relay };
            return true;
        }
        catch (SocketException ex)
        {
            udp4?.Dispose();
            udp6?.Dispose();
            relay?.Dispose();

            if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                sockets = new INetworkInteractions[0];
                return false;
            }

            throw;
        }
    }

    public void Send(DataBox payload)
    {
        bool isIPv4 = payload.Target.AddressFamily == AddressFamily.InterNetwork;
        if (isIPv4)
        {
            bool isRelay = _relayEndPoints.Contains(payload.Target);
            if (isRelay)
            {
                // IPv4 via relay
                _relay?.Send(payload);
            }
            else
            {
                // IPv4 directly
                _udp4.Send(payload);
            }
        }
        else
        {
            // IPv6 directly
            _udp6.Send(payload);
        }
    }

    public bool TryReceive(out DataBox result)
    {
        int offset = RandUtils.Rand.Next(0, 3);

        for (int i = 0; i < 3; i++)
        {
            int turn = (offset + i) % 3;

            if (turn == 0 && _udp4.TryReceive(out result))
            {
                _relayEndPoints.Remove(result!.Target);
                return true;
            }

            if (turn == 1 && _udp6.TryReceive(out result))
            {
                return true;
            }

            if (turn == 2 && _relay.TryReceive(out result))
            {
                if (_relayEndPoints.Count < EpCacheCapacity)
                {
                    _relayEndPoints.Add(result!.Target);
                }
                
                return true;
            }
        }

        result = null!;
        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _udp4?.Dispose();
        _udp6?.Dispose();
        _relay?.Dispose();
    }
}
