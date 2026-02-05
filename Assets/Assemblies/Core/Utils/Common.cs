using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Larnix.Core.Utils
{
    public static class Common
    {
        public const ushort LarnixPort = 27682;

        public const string LoopbackOnlyNickname = "Player";
        public const string LoopbackOnlyPassword = "SGP_PASSWORD\x01";

        public const float FIXED_TIME = 0.02f;
        public const double PARTICLE_VIEW_DISTANCE = 128.0;

        private static List<string> reservedFolders = new List<string>
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
            "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
        };

        public static bool IsValidWorldName(string worldName) =>
        worldName != null &&
            !worldName.Contains('\0') &&
            worldName.Length is >= 1 and <= 32 &&
            worldName.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ') &&
            worldName == worldName.Trim() &&
            !reservedFolders.Contains(worldName.ToUpperInvariant());

        private static readonly ThreadLocal<System.Random> ThreadRandom = new(() => new System.Random());
        public static System.Random Rand()
        {
            return ThreadRandom.Value;
        }

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
    }
}
