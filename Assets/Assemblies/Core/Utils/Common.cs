using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public const string DEFAULT_RELAY_ADDRESS = "relay.se3.page";

        public const string LOOPBACK_ONLY_NICKNAME = "Player";
        public const string LOOPBACK_ONLY_PASSWORD = "SGP_PASSWORD\x01";

        public const float FIXED_TIME = 0.02f;
        public const double PARTICLE_VIEW_DISTANCE = 128.0;
        
        public static Vec2 UP_EPSILON => new Vec2(0.00, 0.01);

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

        public static void DoForSeconds(double seconds, Action<Stopwatch, double> action)
        {
            var timer = Stopwatch.StartNew();
            action(timer, seconds);
            timer.Stop();
        }
    }
}
