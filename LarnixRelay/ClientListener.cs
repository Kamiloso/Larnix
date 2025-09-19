using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Threading;

namespace Larnix.Relay
{
    public class ClientListener : IDisposable
    {
        public readonly IPEndPoint ServerEndPoint;
        public readonly UdpClient _udpClient;
        public readonly ushort Port;

        private ExpiringSet<IPEndPoint> EndPointBag = new ExpiringSet<IPEndPoint>(
            ttl: TimeSpan.FromSeconds(15),
            cleanupInterval: TimeSpan.FromSeconds(5)
            );

        private double? _lastRefresh = null;
        private static Stopwatch _stopwatch = Stopwatch.StartNew();
        private static object _lockRefresh = new object();

        private bool _disposed;

        public ClientListener(IPEndPoint serverEP) // THROWS NoAvailablePortException - no need to Dispose() after that
        {
            ServerEndPoint = serverEP;
            ushort preferredPort = PreferredPort(serverEP);
            Port = Slots.ReserveClientPort(preferredPort);
            _udpClient = UdpClientExtensions.CreateClient(Port, Config.ReceiveBufferClient, Config.SendBufferClient);

            KeepAlive(); // initial keep alive
            Task.Run(() => StartNetworkLoop());
        }

        private async Task StartNetworkLoop()
        {
            while (true)
            {
                try
                {
                    UdpReceiveResult result = await _udpClient.ReceiveAsync();
                    HandleClientPacket(result.RemoteEndPoint, result.Buffer);
                }
                catch (ObjectDisposedException)
                {
                    return; // problems --> stop loop
                }
                catch (Exception ex)
                {
                    if (ex is SocketException sx && sx.SocketErrorCode == SocketError.ConnectionReset)
                        continue;

                    Console.WriteLine($"Client Receive Error: {ex.Message}");
                }
            }
        }

        public async Task SendSafeAsync(byte[] data, IPEndPoint target)
        {
            if (data.Length > Config.MaxMessageLength)
                return; // drop, message too long

            if (EndPointBag.Contains(target))
                await _udpClient.SendAsync(data, data.Length, target);
        }

        public void KeepAlive()
        {
            lock (_lockRefresh)
            {
                _lastRefresh = _stopwatch.Elapsed.TotalMilliseconds;
            }
        }

        public bool ShouldBeAlive()
        {
            lock (_lockRefresh )
            {
                return _stopwatch.Elapsed.TotalMilliseconds < (double)_lastRefresh + Config.ServerLifetime;
            }
        }

        private void HandleClientPacket(IPEndPoint clientEP, byte[] data)
        {
            if (data.Length > Config.MaxMessageLength)
                return; // drop, message too long

            int add = data.Length + 6;
            if (!Program.AllowRelay(ServerEndPoint, add))
                return; // drop, transmission limit reached

            byte[] header = clientEP.SerializeIPv4();
            byte[] send = header.Concat(data).ToArray();

            EndPointBag.Add(clientEP);
            _ = Program.ServerSocket.SendAsync(send, send.Length, ServerEndPoint);
        }

        private static ushort PreferredPort(IPEndPoint endPoint)
        {
            byte[] addressBytes = endPoint.Address.GetAddressBytes();
            byte[] hashSource = new byte[3];
            Array.Copy(addressBytes, 0, hashSource, 0, 3);

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(hashSource);
                return (ushort)(BitConverter.ToUInt32(hash, 0) % (Config.MaxPort - Config.MinPort + 1) + Config.MinPort);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _udpClient?.Dispose();
                Slots.DisposeClientPort(Port);
                _disposed = true;
            }
        }
    }
}
