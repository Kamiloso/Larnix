using System.Collections.Generic;
using System;
using System.Net.Sockets;
using System.Net;

namespace Larnix.Relay
{
    public class NoAvailablePortException : Exception
    {
        public NoAvailablePortException() : base("No available ports.") { }
    }

    public static class UdpClientExtensions
    {
        public static UdpClient CreateClient(ushort port, int recvBuffer, int sendBuffer)
        {
            UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            client.Client.ReceiveBufferSize = recvBuffer;
            client.Client.SendBufferSize = sendBuffer;
            return client;
        }
    }

    public static class IPEndPointExtensions
    {
        public static byte[] SerializeIPv4(this IPEndPoint ep)
        {
            if (ep.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                throw new NotSupportedException("Only IPv4 is supported.");

            byte[] bytes = new byte[6];
            byte[] addressBytes = ep.Address.GetAddressBytes(); // 4 bytes
            Array.Copy(addressBytes, 0, bytes, 0, 4);

            ushort port = (ushort)ep.Port;
            bytes[4] = (byte)(port >> 8);   // big endian
            bytes[5] = (byte)(port & 0xFF);

            return bytes;
        }

        public static IPEndPoint DeserializeIPv4(byte[] data, int offset = 0)
        {
            if (data.Length + offset < 6)
                throw new ArgumentException("Data must be at least 6 bytes.");

            var address = new IPAddress(new byte[] { data[offset + 0], data[offset + 1], data[offset + 2], data[offset + 3] });
            ushort port = (ushort)((data[offset + 4] << 8) | data[offset + 5]);

            return new IPEndPoint(address, port);
        }
    }
}
