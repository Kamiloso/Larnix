using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QuickNet.Channel
{
    public class UdpClient2 : IDisposable
    {
        public readonly ushort Port;
        public readonly bool IsListener;
        public readonly bool IsLoopback;
        public readonly bool IsIPv6;

        private readonly UdpClient _udpClient;
        private readonly IPEndPoint _destination;
        private readonly Thread _thread;
        private readonly ConcurrentQueue<(IPEndPoint, byte[])> _sendQueue = new();
        private readonly ConcurrentQueue<(IPEndPoint, byte[])> _recvQueue = new();
        private readonly int _maxQueueLength;

        private volatile bool _stop = false;
        private bool _disposed = false;

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
                _udpClient.Client.Blocking = false;
                if (IsIPv6) _udpClient.Client.DualMode = false;

                _udpClient.Client.Bind(new IPEndPoint(IsLoopback ?
                    (IsIPv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback) :
                    (IsIPv6 ? IPAddress.IPv6Any : IPAddress.Any),
                    port));

                Port = (ushort)((IPEndPoint)_udpClient.Client.LocalEndPoint).Port;
            }
            catch
            {
                _udpClient?.Dispose();
                throw;
            }

            _thread = new Thread(() => NetworkLoop());
            _thread.Start();
        }

        private void NetworkLoop()
        {
            while (true)
            {
                try
                {
                    while (_sendQueue.Count > _maxQueueLength) _sendQueue.TryDequeue(out _);
                    while (_recvQueue.Count > _maxQueueLength) _recvQueue.TryDequeue(out _);

                    if (_stop)
                    {
                        FlushSending();
                        return;
                    }

                    FlushSending();
                    FlushReceiving();

                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    if (ex is SocketException sx)
                    {
                        if (sx.SocketErrorCode == SocketError.WouldBlock ||
                            sx.SocketErrorCode == SocketError.ConnectionReset)
                            continue;
                    }

                    QuickNet.Debug.LogError(ex.Message);
                    break;
                }
            }
        }

        private void FlushReceiving()
        {
            while (_udpClient.Available > 0)
            {
                IPEndPoint endPoint = null;
                byte[] data = _udpClient.Receive(ref endPoint);

                if (IsListener || _destination.Equals(endPoint))
                    _recvQueue.Enqueue((endPoint, data));
            }
        }

        private void FlushSending()
        {
            while (_sendQueue.TryDequeue(out var send))
            {
                IPEndPoint endPoint = send.Item1;
                byte[] data = send.Item2;

                if (IsListener || _destination.Equals(endPoint))
                    _udpClient.Send(data, data.Length, endPoint);
            }
        }

        public void Send(IPEndPoint endPoint, byte[] data)
        {
            _sendQueue.Enqueue((endPoint, data));
        }

        public bool TryReceive(out (IPEndPoint endPoint, byte[] data) result)
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
            if (!_disposed)
            {
                _disposed = true;

                _stop = true;
                _thread.Join();
                _udpClient?.Dispose();
            }
        }
    }
}
