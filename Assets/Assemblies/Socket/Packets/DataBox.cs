using Larnix.Core.Utils;
using System;
using System.Net;

namespace Larnix.Socket.Packets
{
    internal class DataBox
    {
        public readonly IPEndPoint target;
        public readonly byte[] data;

        public DataBox(IPEndPoint target, byte[] data)
        {
            this.target = target;
            this.data = data;
        }

        public byte[] SerializeV4()
        {
            byte[] addrBytes = target.Address.GetAddressBytes(); // big endian
            byte[] portBytes = { (byte)((target.Port & 0xFF_00) >> 8), (byte)(target.Port & 0xFF) }; // big endian
            return ArrayUtils.MegaConcat(addrBytes, portBytes, data);
        }

        public static bool TryDeserializeV4(byte[] data, out DataBox dataBox)
        {
            if (data.Length < 6)
            {
                dataBox = null;
                return false;
            }

            dataBox = new DataBox(
                new IPEndPoint(
                    new IPAddress(new ReadOnlySpan<byte>(data, 0, 4)),
                    (ushort)((data[4] << 8) | data[5])
                    ),
                data[6..]
                );

            return true;
        }
    }
}
