using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Larnix
{
    public static class Common
    {
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

        public static int ManhattanDistance(Vector2Int v1, Vector2Int v2)
        {
            return Math.Abs(v1.x - v2.x) + Math.Abs(v1.y - v2.y);
        }

        public static float InSquareDistance(Vector2 v1, Vector2 v2)
        {
            return Math.Max(Math.Abs(v1.x - v2.x), Math.Abs(v1.y - v2.y));
        }

        public static Vector2 ReduceIntoSquare(Vector2 center, Vector2 point, float size)
        {
            if (InSquareDistance(center, point) <= size)
                return point;

            if (point.x > center.x + size)
                point = new Vector2(center.x + size, point.y);

            if (point.x < center.x - size)
                point = new Vector2(center.x - size, point.y);

            if (point.y > center.y + size)
                point = new Vector2(point.x, center.y + size);

            if (point.y < center.y - size)
                point = new Vector2(point.x, center.y - size);

            return point;
        }

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

        public static long GetSeedFromString(string input)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToInt64(hash, 0);
        }
    }
}
