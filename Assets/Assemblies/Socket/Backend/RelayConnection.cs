using Socket.Channel;
using Socket.Frontend;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Linq;

namespace Socket
{
    internal class RelayConnection : IDisposable
    {
        public const ushort RELAY_PORT = 27681;

        private readonly IPEndPoint _endPoint;
        private readonly UdpClient2 _udpClient;

        private const int MAX_REVERTER_ENTRIES = 1 << 16;
        private readonly Dictionary<IPEndPoint, (IPEndPoint endPoint, long timestamp)> ExperimentalReverter = new();

        public IPEndPoint EndPoint => new IPEndPoint(_endPoint.Address, _endPoint.Port);
        public ushort RemotePort { get; private set; } // relay port to connect to

        internal static async Task<RelayConnection> EstablishRelayAsync(string address)
        {
            IPEndPoint endPoint = await Resolver.ResolveStringAsync(address, RELAY_PORT);
            if (endPoint == null) return null;

            UdpClient2 udpClient = new UdpClient2(
                port: 0,
                isListener: false,
                isLoopback: IPAddress.IsLoopback(endPoint.Address),
                isIPv6: endPoint.AddressFamily == AddressFamily.InterNetworkV6,
                recvBufferSize: 1024 * 1024,
                destination: endPoint
                );
            RelayConnection relay = new RelayConnection(endPoint, udpClient);

            udpClient.Send(endPoint, new byte[] { 0x01 });
            bool success = false;

            long timeNow = Timestamp.GetTimestamp();
            long deadline = timeNow + 1500; // + 1500 miliseconds

            while (Timestamp.GetTimestamp() < deadline)
            {
                while (udpClient.TryReceive(out var item))
                {
                    IPEndPoint remoteEP = item.endPoint;
                    byte[] bytes = item.data;

                    if (bytes.Length == 2)
                    {
                        success = true;
                        relay.RemotePort = (ushort)(bytes[0] << 8 | bytes[1]);
                        goto finalize;
                    }
                }
                await Task.Delay(100);
            }

        finalize:
            if (success)
            {
                return relay;
            }
            else
            {
                relay.Dispose();
                return null;
            }
        }

        private RelayConnection(IPEndPoint endPoint, UdpClient2 udpClient)
        {
            _endPoint = endPoint;
            _udpClient = udpClient;
        }

        internal void KeepAlive()
        {
            // clean dictionary
            foreach (var vkp in ExperimentalReverter.ToList())
            {
                var key = vkp.Key;
                var value = vkp.Value;

                if (!Timestamp.InTimestamp(value.timestamp))
                    ExperimentalReverter.Remove(key);
            }

            // keep alive
            _udpClient.Send(_endPoint, new byte[] { 0x00 });
        }

        internal void Send(IPEndPoint client, byte[] bytes)
        {
            IPEndPoint remoteEP = FromClassE(client);
            bytes = ArrayUtils.MegaConcat(
                SerializeEndPoint(remoteEP),
                bytes
                );

            _udpClient.Send(_endPoint, bytes);
        }

        internal bool TryReceive(out (IPEndPoint endPoint, byte[] data) result)
        {
            if (_udpClient.TryReceive(out var item))
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
            if (bytes == null || bytes.Length < 6)
                throw new ArgumentException("Bytes should be at least 6 bytes long!");

            remoteEP = DeserializeEndPoint(bytes);
            remoteEP = ToClassE(remoteEP);
            return bytes[6..];
        }

        private IPEndPoint ToClassE(IPEndPoint endPoint)
        {
            if (endPoint.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("Only IPv4 allowed!");

            byte[] addressBytes = endPoint.Address.GetAddressBytes();
            addressBytes[0] |= 0xF0;
            IPEndPoint ArtificialEndPoint = new IPEndPoint(new IPAddress(addressBytes), endPoint.Port);

            if (ExperimentalReverter.Count < MAX_REVERTER_ENTRIES)
            {
                ExperimentalReverter[ArtificialEndPoint] = (endPoint, Timestamp.GetTimestamp());
            }

            return ArtificialEndPoint;
        }

        private IPEndPoint FromClassE(IPEndPoint endPoint)
        {
            if (ExperimentalReverter.TryGetValue(endPoint, out var tuple))
                return tuple.endPoint;

            return new IPEndPoint(IPAddress.Any, 0);
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

        public void Dispose()
        {
            _udpClient.Send(_endPoint, new byte[] { 0x02 });
            _udpClient.Dispose();
        }
    }
}
