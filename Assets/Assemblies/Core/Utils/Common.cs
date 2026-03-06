using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Larnix.Core.Vectors;

namespace Larnix.Core.Utils
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
        public static Vec2 UpEpsilon => new Vec2(0.00, 0.01);

        private static readonly ThreadLocal<Random> _threadRandom = new(() => new Random());
        public static Random Rand() => _threadRandom.Value;

        public static long GetSecureLong()
        {
            var buffer = new byte[8];
            RandomNumberGenerator.Fill(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        public static byte[] GetSecureBytes(int size)
        {
            var buffer = new byte[size];
            RandomNumberGenerator.Fill(buffer);
            return buffer;
        }

        public static long GetSeedFromString(string input)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToInt64(hash, 0);
        }

        public static string SplitPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var step1 = Regex.Replace(input, @"([A-Z])(?=[A-Z][a-z])", "$1 ");
            return Regex.Replace(step1, @"(?<=[a-z])(?=[A-Z])", " ");
        }

        public static string FormatUdpAddress(string address, ushort port)
        {
            UriBuilder uri = new UriBuilder("udp://" + address)
            {
                Port = port
            };

            return uri.ToString()
                .Replace("udp://", "")
                .Replace("/", "");
        }

        public static IPEndPoint RandomClassE()
        {
            Random rand = Rand();
            Span<byte> bytes = stackalloc byte[]
            {
                (byte)rand.Next(240, 256),
                (byte)rand.Next(0, 256),
                (byte)rand.Next(0, 256),
                (byte)rand.Next(0, 256),
            };

            IPAddress address = new IPAddress(bytes);
            int port = rand.Next(1, 65536);

            return new IPEndPoint(address, port);
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
