using System.Linq;
using System.Net;
using System.Net.Sockets;
using System;

namespace QuickNet
{
    public class InternetID
    {
        private static int MaskIPv4;
        private static int MaskIPv6;
        private static bool MasksInitialized = false;

        public readonly IPAddress Address;
        public readonly bool IsIPv6;

        public static void InitializeMasks(int maskIPv4 = 32, int maskIPv6 = 56)
        {
            MaskIPv4 = maskIPv4;
            MaskIPv6 = maskIPv6;
            MasksInitialized = true;
        }

        public InternetID(IPAddress address)
        {
            if (!MasksInitialized)
                throw new System.InvalidOperationException("You must initialize IP masks using static InternetID.InitializeMasks(int, int) method.");

            Address = address;

            if (address.AddressFamily == AddressFamily.InterNetwork)
                IsIPv6 = false;

            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
                IsIPv6 = true;

            else throw new ArgumentException("Unsupported address type! Only IPv4 and IPv6 are accepted.");
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
