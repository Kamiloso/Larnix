#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Larnix.Core.Vectors;

namespace Larnix.GameCore.Utils
{
    public static class Common
    {
        public const ushort LARNIX_PORT = 27682;

        public static string DefaultRelayAddress => "relay.se3.page";
        public static string ReservedNickname => "Player";
        public static string ReservedPassword => "SGP_PASSWORD\x01";

        public static int TargetTPS => 50;
        public static float FixedTime => 1f / TargetTPS;
        public static double ParticleViewDistance => 128.0;

        public static Vec2 UpEpsilon => new(0.00, 0.01);

        public static string SplitPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var step1 = Regex.Replace(input, @"([A-Z])(?=[A-Z][a-z])", "$1 ");
            return Regex.Replace(step1, @"(?<=[a-z])(?=[A-Z])", " ");
        }

        public static string FormatAddress(string address, ushort port)
        {
            UriBuilder uri = new("udp://" + address)
            {
                Port = port
            };

            return uri.ToString()
                .Replace("udp://", "")
                .Replace("/", "");
        }

        public static bool AreSameDirectory(string dir1, string dir2)
        {
            if (string.IsNullOrWhiteSpace(dir1) || string.IsNullOrWhiteSpace(dir2))
                return false;

            string full1 = Path.GetFullPath(dir1);
            string full2 = Path.GetFullPath(dir2);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return string.Equals(full1, full2, StringComparison.OrdinalIgnoreCase);
            else
                return string.Equals(full1, full2, StringComparison.Ordinal);
        }

        public static bool IsInNetworkString(IPAddress address, string networkString)
        {
            int index = networkString.IndexOf('/');

            if (index == -1) // single IP
            {
                return IPAddress.TryParse(networkString, out IPAddress singleIP) &&
                    singleIP.Equals(address);
            }

            ReadOnlySpan<char> ipSpan = networkString.AsSpan(0, index);
            ReadOnlySpan<char> prefixSpan = networkString.AsSpan(index + 1);

            if (IPAddress.TryParse(ipSpan, out IPAddress networkIp) &&
                address.AddressFamily == networkIp.AddressFamily &&
                int.TryParse(prefixSpan, out int prefixLength))
            {
                int maxPrefix = networkIp.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
                if (prefixLength >= 0 && prefixLength <= maxPrefix)
                {
                    int fullBytes = prefixLength / 8;
                    int remainingBits = prefixLength % 8;

                    Span<byte> networkBytes = stackalloc byte[16];
                    Span<byte> addressBytes = stackalloc byte[16];

                    networkIp.TryWriteBytes(networkBytes, out _);
                    address.TryWriteBytes(addressBytes, out _);

                    for (int i = 0; i < fullBytes; i++)
                    {
                        if (networkBytes[i] != addressBytes[i])
                            return false;
                    }

                    if (remainingBits > 0)
                    {
                        int mask = (0xFF << (8 - remainingBits)) & 0xFF;
                        if ((networkBytes[fullBytes] & mask) != (addressBytes[fullBytes] & mask))
                            return false;
                    }

                    return true;
                }
            }
            
            return false;
        }

        public static void DoForSeconds(double seconds, Action<Stopwatch, double> action)
        {
            var timer = Stopwatch.StartNew();
            action(timer, seconds);
            timer.Stop();
        }
    }
}
