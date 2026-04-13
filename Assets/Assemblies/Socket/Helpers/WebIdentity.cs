#nullable enable
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System;
using Larnix.Core.Utils;

namespace Larnix.Socket.Helpers;

internal class WebIdentity
{
    private readonly IPAddress _address;
    private readonly int _subnet;

    public bool IsIPv4 { get; }

    public WebIdentity(IPAddress address, int subnet)
    {
        _address = address;
        _subnet = subnet;
        IsIPv4 = address.AddressFamily == AddressFamily.InterNetwork;
    }

    public override string ToString()
    {
        byte[] masked = MaskBytes(_address.GetAddressBytes(), _subnet);
        IPAddress network = new(masked);
        return $"{network}/{_subnet}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is WebIdentity other)
        {
            if (IsIPv4 == other.IsIPv4 &&
                _subnet == other._subnet)
            {
                return MasksCompare(_address, other._address, _subnet);
            }
        }
        return false;
    }

    public override int GetHashCode()
    {
        byte[] bytes = MaskBytes(_address.GetAddressBytes(), _subnet);

        unchecked
        {
            int hash = 17;
            foreach (byte b in bytes)
            {
                hash = hash * 31 + b;
            }
            return hash;
        }
    }

    public static bool operator ==(WebIdentity? left, WebIdentity? right)
    {
        if (left is null && right is null)
            return true;

        return left?.Equals(right) ?? false;
    }

    public static bool operator !=(WebIdentity? left, WebIdentity? right)
    {
        return !(left == right);
    }

    private static byte[] MaskBytes(byte[] bytes, int n)
    {
        if (n < 0) n = 0;
        if (n > bytes.Length * 8) n = bytes.Length * 8;

        int total_bytes = bytes.Length;
        int full_bytes = n / 8;
        int remaining_bits = n % 8;

        if (full_bytes == total_bytes)
            return bytes.ToArray();

        byte remaining_byte = 0;
        if (remaining_bits > 0)
        {
            byte remaining_mask = (byte)(0xFF << (8 - remaining_bits));
            remaining_byte = (byte)(remaining_mask & bytes[full_bytes]);
        }

        byte[] bytes1 = bytes[0..full_bytes];
        byte[] bytes2 = remaining_bits > 0 ? new byte[] { remaining_byte } : new byte[0];
        byte[] bytes3 = remaining_bits > 0 ? new byte[total_bytes - full_bytes - 1] : new byte[total_bytes - full_bytes];

        return ArrayUtils.MegaConcat(bytes1, bytes2, bytes3);
    }

    private static bool MasksCompare(IPAddress ip1, IPAddress ip2, int mask)
    {
        byte[] mask1 = MaskBytes(ip1.GetAddressBytes(), mask);
        byte[] mask2 = MaskBytes(ip2.GetAddressBytes(), mask);
        return mask1.SequenceEqual(mask2);
    }
}
