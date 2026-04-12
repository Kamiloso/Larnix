#nullable enable
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Larnix.Core;
using Larnix.Model;

namespace Larnix.Socket.Networking;

// WARNING: This class should be fully thread safe!

internal class RelayConnection : INetworkInteractions, IDisposable
{
    private static int RelayTimeout => 1500; // ms
    private static int KeepAliveInterval => 3000; // ms

    public string ForeignAddress { get; private set; } = "";

    private readonly UdpClient2 _udp;
    private readonly IPEndPoint _target;

    private volatile bool _stop = false;
    private volatile int _disposeState = 0;

    private enum RelayInfo : byte
    {
        KeepAlive = 0x00,
        Start = 0x01,
        Stop = 0x02
    };

    private RelayConnection(IPEndPoint endPoint, UdpClient2 udpClient)
    {
        _target = endPoint;
        _udp = udpClient;
    }

    public static async Task<RelayConnection?> EstablishRelayAsync(string address)
    {
        IPEndPoint? target = await DnsResolver.ResolveAsync(address, GameInfo.DefaultRelayPort);
        if (target == null)
        {
            return null;
        }

        UdpClient2 udpClient = new(
            port: 0,
            isListener: false,
            isLoopback: IPAddress.IsLoopback(target.Address),
            isIPv6: target.AddressFamily == AddressFamily.InterNetworkV6,
            recvBufferSize: 1024 * 1024,
            destination: target
            );

        RelayConnection relay = new(target, udpClient);

        long timeNow = Timestamp.Now();
        long deadline = timeNow + RelayTimeout;

        relay.SendInfo(RelayInfo.Start); // send and wait for answer
        while (Timestamp.Now() < deadline)
        {
            while (udpClient.TryReceive(out DataBox result))
            {
                byte[] bytes = result.Data;
                if (bytes.Length == 2)
                {
                    ushort port = (ushort)(bytes[0] << 8 | bytes[1]);
                    relay.ForeignAddress = Common.FormatAddress(address, port);
                    
                    _ = Task.Run(relay.KeepAliveLoop);

                    return relay;
                }
            }

            await Task.Delay(100);
        }

        relay.Dispose();
        return null; // timeout
    }

    public void Send(DataBox payload)
    {
        byte[] relayBytes = RelaySerializer.AsRelayBytes(payload);
        _udp.Send(new DataBox(_target, relayBytes));
    }

    public bool TryReceive(out DataBox result)
    {
        if (_udp.TryReceive(out DataBox inResult))
        {
            DataBox? result1 = RelaySerializer.FromRelayBytes(inResult.Data);
            if (result1 != null)
            {
                result = result1;
                return true;
            }
        }

        result = null!;
        return false;
    }

    private void SendInfo(RelayInfo info)
    {
        DataBox payload = new(_target, new byte[] { (byte)info });
        _udp.Send(payload);
    }

    private async Task KeepAliveLoop()
    {
        while (!_stop)
        {
            // keep alive may theoritically slightly outrun stop,
            // but that's not a problem, relay will handle that

            // UdpClient2 ignores sending after dispose

            SendInfo(RelayInfo.KeepAlive);
            await Task.Delay(KeepAliveInterval);
        }
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposeState, 1, 0) == 0)
        {
            _stop = true;

            SendInfo(RelayInfo.Stop);
            _udp?.Dispose();
        }
    }
}
