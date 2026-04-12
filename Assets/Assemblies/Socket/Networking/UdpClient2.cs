#nullable enable
using System.Net;
using System.Net.Sockets;
using System;
using System.Threading.Tasks;
using System.Threading;
using Larnix.Core;
using Larnix.Core.Collections;

namespace Larnix.Socket.Networking;

// WARNING: This class should be fully thread safe!

internal class UdpClient2 : INetworkInteractions, IDisposable
{
    public ushort Port { get; }
    public bool IsListener { get; }
    public bool IsLoopback { get; }
    public bool IsIPv6 { get; }

    private readonly UdpClient _udpClient;
    private readonly IPEndPoint? _destination;

    private readonly Task _sendLoop;

    private readonly SmartConcurrentQueue<DataBox> _sendQueue = new();
    private readonly SmartConcurrentQueue<DataBox> _recvQueue = new();
    private readonly int _maxQueueLength;

    private volatile bool _stop = false;
    private volatile int _disposedState = 0;

    public UdpClient2(ushort port, bool isListener, bool isLoopback, bool isIPv6, int recvBufferSize, IPEndPoint? destination = null)
    {
        if (destination != null)
        {
            if (destination.AddressFamily != AddressFamily.InterNetwork &&
                destination.AddressFamily != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException("Only IPv4 and IPv6 are supported!");
            }
        }

        if (!isListener && destination == null)
        {
            throw new ArgumentException("Non-listeners must specify a destination IPEndPoint!");
        }

        IsListener = isListener;
        IsLoopback = isLoopback;
        IsIPv6 = isIPv6;

        _destination = destination;
        _maxQueueLength = recvBufferSize / 1024 + 1; // assuming average packet size to be 1024 bytes

        _udpClient = new UdpClient(IsIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork);
        _udpClient.Client.ReceiveBufferSize = recvBufferSize;
        _udpClient.Client.Blocking = true;

        if (IsIPv6)
        {
            _udpClient.Client.DualMode = false;
        }

        _udpClient.Client.Bind(new IPEndPoint(IsLoopback ?
            IsIPv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback :
            IsIPv6 ? IPAddress.IPv6Any : IPAddress.Any,
            port));

        Port = (ushort)((IPEndPoint)_udpClient.Client.LocalEndPoint).Port;

        _sendLoop = Task.Run(SendLoop);
        _ = Task.Run(ReceiveLoop);
    }

    private async Task SendLoop()
    {
        while (true)
        {
            if (!await FlushSendQueue())
            {
                return; // stop loop
            }

            if (_stop)
            {
                await FlushSendQueue();
                return;
            }

            await Task.Delay(1);
        }

        async Task<bool> FlushSendQueue()
        {
            while (_sendQueue.TryDequeue(out var result))
            {
                try
                {
                    byte[] datagram = result!.Data;
                    IPEndPoint target = result.Target;

                    await _udpClient.SendAsync(datagram, datagram.Length, target);
                }
                catch (Exception ex)
                {
                    if (!HandleNetworkException(ex))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    private async Task ReceiveLoop()
    {
        while (true)
        {
            try
            {
                _recvQueue.DropUntilCount(_maxQueueLength);

                var result = await _udpClient.ReceiveAsync();

                IPEndPoint target = result.RemoteEndPoint;
                byte[] bytes = result.Buffer;

                if (_destination == null || _destination.Equals(target))
                {
                    _recvQueue.Enqueue(new DataBox(target, bytes));
                }
            }
            catch (Exception ex)
            {
                if (!HandleNetworkException(ex))
                {
                    return; // stop loop
                }
            }
        }
    }

    private bool HandleNetworkException(Exception ex)
    {
        if (ex is ObjectDisposedException)
        {
            // stop task when ObjectDisposed to prevent "zombie-threads"
            // it is a normal cancellation method
            return false;
        }

        if (ex is SocketException { SocketErrorCode: SocketError.ConnectionReset })
        {
            return true; // connection reset can be ignored
        }

        if (!_stop)
        {
            Echo.LogError(ex.Message);
        }

        return false; // other errors, close socket
    }

    public void Send(DataBox payload)
    {
        if (_disposedState != 0) return;

        _sendQueue.DropUntilCount(_maxQueueLength);
        _sendQueue.Enqueue(payload);
    }

    private volatile int _deqLimit = 0;
    public bool TryReceive(out DataBox result)
    {
        while (true)
        {
            int currentLimit = _deqLimit;
            int nextLimit = currentLimit <= 0
                ? _recvQueue.Count
                : currentLimit - 1;

            if (Interlocked.CompareExchange(ref _deqLimit, nextLimit, currentLimit) == currentLimit)
            {
                if (nextLimit > 0 && _recvQueue.TryDequeue(out result))
                {
                    return true;
                }

                result = null!;
                return false;
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposedState, 1, 0) == 0)
        {
            _stop = true;
            _sendLoop?.Wait(); // wait for sending to ensure that everything is sent before end of Dispose()
            // don't wait for receiving, will close anyway or may be killed - who cares?
            _udpClient?.Dispose();
        }
    }
}
