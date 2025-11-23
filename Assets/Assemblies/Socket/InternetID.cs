using System.Linq;
using System.Net;
using System.Net.Sockets;
using System;

namespace Socket
{
    public class InternetID
    {
        private readonly IPAddress Address;
        private readonly int Subnet;
        private readonly bool IsIPv4;

        public InternetID(IPAddress address, int subnet)
        {
            Address = address;
            Subnet = subnet;
            IsIPv4 = address.AddressFamily == AddressFamily.InterNetwork;
        }

        public static bool operator ==(InternetID left, InternetID right)
        {
            if(left is null && right is null)
                return true;

            return left?.Equals(right) ?? false;
        }

        public static bool operator !=(InternetID left, InternetID right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is InternetID other && IsIPv4 == other.IsIPv4)
            {
                int mask = Math.Min(Subnet, other.Subnet);

                return MasksCompare(Address, other.Address, mask);
            }
            return false;
        }

        public override int GetHashCode()
        {
            byte[] bytes = MaskBytes(Address.GetAddressBytes(), Subnet);

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

            return bytes1.Concat(bytes2).Concat(bytes3).ToArray();
        }

        private static bool MasksCompare(IPAddress ip1, IPAddress ip2, int mask)
        {
            byte[] mask1 = MaskBytes(ip1.GetAddressBytes(), mask);
            byte[] mask2 = MaskBytes(ip2.GetAddressBytes(), mask);
            return mask1.SequenceEqual(mask2);
        }
    }
}
