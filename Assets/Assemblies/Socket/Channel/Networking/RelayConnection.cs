using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Larnix.Socket.Frontend;
using Larnix.Socket.Packets;

namespace Larnix.Socket.Channel.Networking
{
    internal class RelayConnection : INetworkInteractions, IDisposable
    {
        public const ushort RELAY_PORT = 27681;

        private readonly UdpClient2 Udp;
        private readonly IPEndPoint EndPoint;

        public ushort RemotePort { get; private set; } // relay port to connect to

        private bool _disposed;

        private enum RelayInfo : byte
        {
            KeepAlive = 0x00,
            Start = 0x01,
            Stop = 0x02
        };

        private RelayConnection(IPEndPoint endPoint, UdpClient2 udpClient)
        {
            EndPoint = endPoint;
            Udp = udpClient;
        }

        public static async Task<RelayConnection> EstablishRelayAsync(string address)
        {
            IPEndPoint endPoint = await Resolver.ResolveStringAsync(address, RELAY_PORT);
            if (endPoint == null)
                return null;

            UdpClient2 udpClient = new UdpClient2(
                port: 0,
                isListener: false,
                isLoopback: IPAddress.IsLoopback(endPoint.Address),
                isIPv6: endPoint.AddressFamily == AddressFamily.InterNetworkV6,
                recvBufferSize: 1024 * 1024,
                destination: endPoint
                );
            RelayConnection relay = new RelayConnection(endPoint, udpClient);

            long timeNow = Timestamp.GetTimestamp();
            long deadline = timeNow + 1500; // + 1500 miliseconds

            relay.SendInfo(RelayInfo.Start); // send and wait for answer
            while (Timestamp.GetTimestamp() < deadline)
            {
                while (udpClient.TryReceive(out var item))
                {
                    IPEndPoint remoteEP = item.target;
                    byte[] bytes = item.data;

                    if (bytes.Length == 2)
                    {
                        relay.RemotePort = (ushort)(bytes[0] << 8 | bytes[1]);
                        return relay;
                    }
                }
                await Task.Delay(100);
            }

            relay.Dispose();
            return null; // timeout
        }

        public void KeepAlive()
        {
            SendInfo(RelayInfo.KeepAlive);
        }

        public void Send(IPEndPoint remoteEP, byte[] bytes)
        {
            bytes = new DataBox(remoteEP, bytes).SerializeV4();
            Udp.Send(EndPoint, bytes);
        }

        public bool TryReceive(out DataBox result)
        {
            if (Udp.TryReceive(out DataBox wrapItem))
            {
                if (DataBox.TryDeserializeV4(wrapItem.data, out DataBox realItem))
                {
                    result = realItem;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private void SendInfo(RelayInfo info)
        {
            Udp.Send(EndPoint, new byte[] { (byte)info });
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                SendInfo(RelayInfo.Stop); // thread safe
                Udp?.Dispose(); // thread safe
            }
        }
    }
}
