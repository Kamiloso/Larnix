#nullable enable
using Larnix.Core.Utils;
using System.Net.Sockets;
using System.Net;
using System;

namespace Larnix.Socket.Networking;

internal static class RelaySerializer
{
    public static byte[] AsRelayBytes(DataBox payload)
    {
        if (payload.Target.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("Only IPv4 addresses are supported.");

        IPAddress address = payload.Target.Address;
        int port = payload.Target.Port;

        byte[] addrBytes = address.GetAddressBytes();
        byte[] portBytes = new byte[] { (byte)(port >> 8), (byte)(port & 0xFF) };
        byte[] dataBytes = payload.Data;

        return ArrayUtils.MegaConcat(addrBytes, portBytes, dataBytes);
    }

    public static DataBox? FromRelayBytes(byte[] data)
    {
        if (data.Length < 6) return null;

        byte[] addrBytes = data[..4];
        byte[] portBytes = data[4..6];
        byte[] payloadBytes = data[6..];

        IPAddress address = new(addrBytes);
        int port = (portBytes[0] << 8) | portBytes[1];

        IPEndPoint target = new(address, port);

        return new DataBox(target, payloadBytes);
    }
}
