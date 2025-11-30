using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Linq;
using Larnix.Core.Utils;
using Larnix.Socket.Frontend;

namespace Larnix.Socket.UdpClients
{
    internal class RelayConnection : IDisposable
    {
        public const ushort RELAY_PORT = 27681;

        private readonly UdpClient2 Udp;
        private readonly IPEndPoint EndPoint;

        private const int MAP_CAPACITY = 1 << 16; // over 65k
        private readonly Dictionary<IPEndPoint, (IPEndPoint endPoint, long timestamp)> IPMap = new();

        public ushort RemotePort { get; private set; } // relay port to connect to

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
                    IPEndPoint remoteEP = item.endPoint;
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
            IPMapCleanup();
            SendInfo(RelayInfo.KeepAlive);
        }

        public void Send(IPEndPoint client, byte[] bytes)
        {
            IPEndPoint remoteEP;
            if ((remoteEP = FromClassE(client)) != null)
            {
                bytes = ArrayUtils.MegaConcat(
                    SerializeEndPoint(remoteEP),
                    bytes);

                Udp.Send(EndPoint, bytes);
            }
        }

        public bool TryReceive(out (IPEndPoint endPoint, byte[] data) result)
        {
            if (Udp.TryReceive(out var item))
            {
                IPEndPoint remoteEP = item.endPoint;
                byte[] bytes = item.data;

                if (bytes.Length >= 6)
                {
                    bytes = UnpackPayload(bytes, ref remoteEP);
                    result = (remoteEP, bytes);
                    return true;
                }
            }

            result = default;
            return false;
        }

        private byte[] UnpackPayload(byte[] bytes, ref IPEndPoint remoteEP)
        {
            remoteEP = DeserializeEndPoint(bytes, 0);
            remoteEP = ToClassE(remoteEP);
            return bytes[6..];
        }

        private IPEndPoint ToClassE(IPEndPoint endPoint)
        {
            byte[] addressBytes = endPoint.Address.GetAddressBytes();
            addressBytes[0] |= 0xF0;

            IPEndPoint endPointE = new IPEndPoint(
                new IPAddress(addressBytes),
                endPoint.Port);

            if (IPMap.Count < MAP_CAPACITY)
                IPMap[endPointE] = (endPoint, Timestamp.GetTimestamp());

            return endPointE;
        }

        private IPEndPoint FromClassE(IPEndPoint endPoint)
        {
            if (IPMap.TryGetValue(endPoint, out var item))
                return item.endPoint;

            return null;
        }

        private void IPMapCleanup()
        {
            foreach (var vkp in IPMap.ToList())
            {
                long timestamp = vkp.Value.timestamp;

                if (!Timestamp.InTimestamp(timestamp))
                    IPMap.Remove(vkp.Key);
            }
        }

        private static byte[] SerializeEndPoint(IPEndPoint endPoint)
        {
            if (endPoint.AddressFamily != AddressFamily.InterNetwork)
                throw new NotSupportedException("Only IPv4 allowed!");

            byte[] addressBytes = endPoint.Address.GetAddressBytes();
            byte[] result = new byte[6];

            // Big endian address
            Buffer.BlockCopy(addressBytes, 0, result, 0, 4);

            // Big endian port
            ushort port = (ushort)endPoint.Port;
            result[4] = (byte)(port >> 8);
            result[5] = (byte)(port & 0xFF);

            return result;
        }

        private static IPEndPoint DeserializeEndPoint(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 6)
                throw new ArgumentException("Cannot deserialize EndPoint!");

            // Big endian address
            byte[] addressBytes = new byte[4];
            Buffer.BlockCopy(bytes, offset, addressBytes, 0, 4);
            IPAddress address = new IPAddress(addressBytes);

            // Big endian port
            ushort port = (ushort)((bytes[offset + 4] << 8) | bytes[offset + 5]);

            return new IPEndPoint(address, port);
        }

        private void SendInfo(RelayInfo info)
        {
            Udp.Send(EndPoint, new byte[] { (byte)info });
        }

        public void Dispose()
        {
            SendInfo(RelayInfo.Stop);
            Udp.Dispose();
        }
    }
}
