using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Larnix.Socket
{
    public class InternetID
    {
        public static int MaskIPv4 { get; set; } = 32;
        public static int MaskIPv6 { get; set; } = 56;

        public readonly IPAddress Address;
        public readonly bool IsIPv6;

        public InternetID(IPAddress address)
        {
            Address = address;

            if (address.AddressFamily == AddressFamily.InterNetwork)
                IsIPv6 = false;

            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
                IsIPv6 = true;

            else throw new System.ArgumentException("Unsupported address type! Only IPv4 and IPv6 are accepted.");
        }

        public static bool operator ==(InternetID left, InternetID right)
        {
            if(ReferenceEquals(left, right))
                return true;

            if(left is null  || right is null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(InternetID left, InternetID right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is InternetID))
                return false;

            InternetID other = (InternetID)obj;

            if (IsIPv6 != other.IsIPv6)
                return false;

            return MasksCompare(Address, other.Address, IsIPv6 ? MaskIPv6 : MaskIPv4);
        }

        public override int GetHashCode()
        {
            byte[] bytes = MaskBytes(Address.GetAddressBytes(), IsIPv6 ? MaskIPv6 : MaskIPv4);

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

        public static byte[] MaskBytes(byte[] bytes, int n)
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

        public static bool MasksCompare(IPAddress ip1, IPAddress ip2, int mask)
        {
            byte[] mask1 = MaskBytes(ip1.GetAddressBytes(), mask);
            byte[] mask2 = MaskBytes(ip2.GetAddressBytes(), mask);
            return mask1.SequenceEqual(mask2);
        }
    }
}
