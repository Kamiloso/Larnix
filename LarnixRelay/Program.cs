using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Relay
{
    public static class Program
    {
        public static UdpClient ServerSocket => _serverSocket;
        private static UdpClient _serverSocket = null;

        private static readonly Dictionary<IPEndPoint, ClientListener> _Listeners = new Dictionary<IPEndPoint, ClientListener>();
        private static readonly Dictionary<IPEndPoint, int> _RelayedBytes = new Dictionary<IPEndPoint, int>();
        private static readonly Dictionary<IPEndPoint, int> _ControlMsgs = new Dictionary<IPEndPoint, int>();
        private static readonly Dictionary<IPAddress, int> _RegisteredCount = new Dictionary<IPAddress, int>();
        private static int _TotalRegisteredCount = 0;

        private static readonly object _lock = new object();

        private static async Task Main(string[] args)
        {
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Console.WriteLine($"Unobserved: {e.Exception}");
                e.SetObserved();
            };

            using (UdpClient udpServer = UdpClientExtensions.CreateClient(Config.ServerPort, Config.ReceiveBufferServer, Config.SendBufferServer))
            {
                _serverSocket = udpServer;
                Console.WriteLine($"Relay started! Running on port {Config.ServerPort}...");

                _ = PeriodicTask_100ms();
                _ = PeriodicTask_1s();

                while (true)
                {
                    try
                    {
                        var result = await udpServer.ReceiveAsync();
                        IPEndPoint remoteEP = result.RemoteEndPoint;
                        byte[] data = result.Buffer;

                        if (data.Length == 1)
                        {
                            lock (_lock)
                            {
                                _ControlMsgs.TryGetValue(remoteEP, out int amount);

                                const int MaxControlMsgsPerSecond = 6;
                                if (amount >= MaxControlMsgsPerSecond)
                                    continue; // drop, too many control packets

                                amount++;
                                _ControlMsgs[remoteEP] = amount;
                            }

                            byte code = data[0];
                            switch (code)
                            {
                                case 0x00: HandleKeepAlive(remoteEP); break;
                                case 0x01: HandleAddServer(remoteEP); break;
                                case 0x02: _ = HandleRemoveServer(remoteEP); break;
                            }
                        }

                        if (data.Length >= 6)
                        {
                            HandleServerMessage(remoteEP, data);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        continue; // problems --> continue receiving
                    }
                    catch (Exception ex)
                    {
                        if (ex is SocketException sx)
                        {
                            if (sx.SocketErrorCode == SocketError.ConnectionReset)
                                continue;
                        }

                        Console.WriteLine($"Server Receive Error: {ex.Message}");
                        continue;
                    }
                }
            }
        }

        private static async Task PeriodicTask_100ms()
        {
            while (true)
            {
                try
                {
                    lock (_lock) // once per 100 ms
                    {
                        _RelayedBytes.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Periodic error 100ms: " + ex.Message);
                }

                await Task.Delay(100);
            }
        }

        private static async Task PeriodicTask_1s()
        {
            while (true)
            {
                try
                {
                    lock (_lock) // once per second
                    {
                        foreach (var vkp in _Listeners.ToList())
                        {
                            var key = vkp.Key;
                            var value = vkp.Value;

                            if (!value.ShouldBeAlive())
                                _ = HandleRemoveServer(key);
                        }

                        _ControlMsgs.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Periodic error 1s: " + ex.Message);
                }

                await Task.Delay(1000);
            }
        }

        public static bool AllowRelay(IPEndPoint serverEP, int add)
        {
            lock (_lock)
            {
                if (_TotalRegisteredCount == 0)
                    return false;

                _RelayedBytes.TryGetValue(serverEP, out int amount);
                if (amount + add <= Config.MaxTransferPerSecond / 10 / _TotalRegisteredCount) // resets every 100 ms
                {
                    amount += add;
                    _RelayedBytes[serverEP] = amount;
                    return true;
                }
                return false;
            }
        }

        private static void HandleKeepAlive(IPEndPoint serverEP)
        {
            // also try adding server if not added for some reason
            // it should recreate connection in most cases, for example
            // after relay restarts
            HandleAddServer(serverEP);

            lock (_lock)
            {
                if (_Listeners.TryGetValue(serverEP, out var listener))
                {
                    listener.KeepAlive();
                    _ = _serverSocket.SendAsync(Array.Empty<byte>(), 0, serverEP);
                }
            }
        }

        private static bool HandleAddServer(IPEndPoint serverEP)
        {
            lock (_lock)
            {
                if (!_Listeners.ContainsKey(serverEP) &&
                _TotalRegisteredCount < Config.MaxServersGlobally)
                {
                    _RegisteredCount.TryGetValue(serverEP.Address, out int regs);
                    if (regs < Config.MaxServersPerIP)
                    {
                        try
                        {
                            ClientListener listener = new ClientListener(serverEP);
                            _Listeners.Add(serverEP, listener);

                            ushort port = listener.Port;
                            byte[] portBytes = new byte[] { (byte)(port >> 8), (byte)port };
                            _ = _serverSocket.SendAsync(portBytes, portBytes.Length, serverEP);

                            _TotalRegisteredCount++;
                            _RegisteredCount[serverEP.Address] = ++regs;

                            Console.WriteLine($"Assigned port {port} to {serverEP}");
                            return true;
                        }
                        catch (NoAvailablePortException)
                        {
                            Console.WriteLine("Cannot assign any client port. All are in use!");
                        }
                    }
                }

                return false;
            }
        }

        private static async Task HandleRemoveServer(IPEndPoint serverEP)
        {
            await Task.Delay(250); // to ensure all ending packets are sent properly

            lock (_lock)
            {
                if (_Listeners.TryGetValue(serverEP, out var listener))
                {
                    ushort port = listener.Port;
                    _Listeners.Remove(serverEP);
                    listener.Dispose();

                    _TotalRegisteredCount--;
                    int regs = _RegisteredCount[serverEP.Address] - 1;
                    if (regs > 0) _RegisteredCount[serverEP.Address] = regs;
                    else _RegisteredCount.Remove(serverEP.Address);

                    Console.WriteLine($"Port {port} returned to poll.");
                }
            }
        }

        private static void HandleServerMessage(IPEndPoint serverEP, byte[] data)
        {
            if (data.Length < 6)
                return; // drop, too short packet

            if (data.Length - 6 > Config.MaxMessageLength)
                return; // drop, message too long

            int add = data.Length - 6;
            if (!AllowRelay(serverEP, add))
                return; // drop, transmission limit reached

            lock (_lock)
            {
                if (_Listeners.TryGetValue(serverEP, out var listener))
                {
                    IPEndPoint target = IPEndPointExtensions.DeserializeIPv4(data, 0);

                    byte[] send = new byte[data.Length - 6];
                    Array.Copy(data, 6, send, 0, send.Length);

                    Task.Run(async () =>
                    {
#pragma warning disable CS0162
                        if (Config.ArtificialPing != 0)
                            await Task.Delay(Config.ArtificialPing);

                        if (Config.ArtificialJitter != 0)
                            await Task.Delay(new Random().Next(0, Config.ArtificialJitter));
#pragma warning restore CS0162

                        _ = listener.SendSafeAsync(send, target);
                    });
                }
            }
        }
    }
}
