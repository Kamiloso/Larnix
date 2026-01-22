using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System;
using System.Threading.Tasks;
using Larnix.Socket.Packets;
using System.Threading;

namespace Larnix.Socket.Channel.Networking
{
    internal class UdpClient2 : INetworkInteractions, IDisposable
    {
        public readonly ushort Port;
        public readonly bool IsListener;
        public readonly bool IsLoopback;
        public readonly bool IsIPv6;

        private readonly UdpClient _udpClient;
        private readonly IPEndPoint _destination;
        private readonly Task _sendLoop;
        private readonly Task _recvLoop;
        private readonly ConcurrentQueue<DataBox> _sendQueue = new();
        private readonly ConcurrentQueue<DataBox> _recvQueue = new();
        private readonly int _maxQueueLength;

        private volatile bool _stop;
        private volatile int _disposedState = 0;

        public UdpClient2(ushort port, bool isListener, bool isLoopback, bool isIPv6, int recvBufferSize, IPEndPoint destination = null)
        {
            if (destination != null)
            {
                if (destination.AddressFamily != AddressFamily.InterNetwork &&
                    destination.AddressFamily != AddressFamily.InterNetworkV6)
                    throw new ArgumentException("Only IPv4 and IPv6 are supported!");
            }

            if (!isListener && destination == null)
                throw new ArgumentException("Non-listeners must specify a destination IPEndPoint!");

            IsListener = isListener;
            IsLoopback = isLoopback;
            IsIPv6 = isIPv6;

            _destination = destination;
            _maxQueueLength = recvBufferSize / 1024 + 1; // assuming average packet size to be 1024 bytes

            try
            {
                _udpClient = new UdpClient(IsIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork);
                _udpClient.Client.ReceiveBufferSize = recvBufferSize;
                _udpClient.Client.Blocking = true;
                if (IsIPv6) _udpClient.Client.DualMode = false;

                _udpClient.Client.Bind(new IPEndPoint(IsLoopback ?
                    IsIPv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback :
                    IsIPv6 ? IPAddress.IPv6Any : IPAddress.Any,
                    port));

                Port = (ushort)((IPEndPoint)_udpClient.Client.LocalEndPoint).Port;
            }
            catch
            {
                _udpClient?.Dispose();
                throw;
            }

            _sendLoop = Task.Run(() => SendLoop());
            _recvLoop = Task.Run(() => ReceiveLoop());
        }

        private async Task SendLoop()
        {
            while (true)
            {
                if(!await _SendAll())
                    return; // stop loop

                if (_stop)
                {
                    await _SendAll();
                    return;
                }

                await Task.Delay(1);
            }
        }

        private async Task<bool> _SendAll()
        {
        retry:
            try
            {
                while (_sendQueue.TryDequeue(out var result))
                {
                    IPEndPoint endPoint = result.target;
                    byte[] bytes = result.data;

                    await _udpClient.SendAsync(bytes, bytes.Length, endPoint);
                }
            }
            catch (Exception ex)
            {
                if (!HandleNetworkException(ex))
                    return false;

                goto retry;
            }

            return true;
        }

        private async Task ReceiveLoop()
        {
            while (true)
            {
                try
                {
                    while (_recvQueue.Count > _maxQueueLength)
                        _recvQueue.TryDequeue(out _); // discard old packets

                    var result = await _udpClient.ReceiveAsync();
                    IPEndPoint remoteEP = result.RemoteEndPoint;
                    byte[] bytes = result.Buffer;

                    if (_destination == null || _destination.Equals(remoteEP))
                    {
                        _recvQueue.Enqueue(new DataBox(remoteEP, bytes));
                    }
                }
                catch (Exception ex)
                {
                    if (!HandleNetworkException(ex))
                        return; // stop loop
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

            if (ex is SocketException sx)
            {
                if (sx.SocketErrorCode == SocketError.ConnectionReset)
                    return true; // connection reset can be ignored
            }

            if (!_stop)
                Core.Debug.LogError(ex.ToString());

            return false; // other errors, close socket
        }

        public void Send(IPEndPoint remoteEP, byte[] data)
        {
            while (_sendQueue.Count > _maxQueueLength)
                _sendQueue.TryDequeue(out _); // discard packets when sending too many

            _sendQueue.Enqueue(new DataBox(remoteEP, data));
        }

        public bool TryReceive(out DataBox result)
        {
            if (_recvQueue.TryDequeue(out var _result))
            {
                result = _result;
                return true;
            }

            result = default;
            return false;
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
}
