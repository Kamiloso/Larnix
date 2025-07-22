using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Unity.Burst.Intrinsics;
using UnityEngine;

namespace Larnix
{
    public static class Common
    {
        public static string GAME_VERSION = "0.0.1";
        public const uint GAME_VERSION_UINT = 1;

        public static bool IsGoodNickname(string nickname) =>
            !nickname.Contains('\0') &&
            nickname.Length is >= 3 and <= 16 &&
            nickname.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');

        public static bool IsGoodPassword(string password) =>
            !password.Contains('\0') &&
            password.Length is >= 7 and <= 32;

        public static bool IsGoodMessage(string message) =>
            !message.Contains('\0') &&
            message.Length <= 256;

        public static byte[] StringToFixedBinary(string str, int stringSize)
        {
            int bytesSize = sizeof(char) * stringSize;
            byte[] bytes = Encoding.Unicode.GetBytes(str);

            if (bytes.Length > bytesSize)
                return bytes[0..bytesSize];
            else
                return bytes.Concat(new byte[bytesSize - bytes.Length]).ToArray();
        }

        public static string FixedBinaryToString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            if (bytes.Length % 2 != 0)
                throw new ArgumentException("Invalid byte array length for UTF-16 string.");

            return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
        }

        public static float GridDistance(Vector2 v1, Vector2 v2)
        {
            Vector2 diff = v1 - v2;
            return Math.Abs(diff.x) + Math.Abs(diff.y);
        }

        public static float InSquareDistance(Vector2 v1, Vector2 v2)
        {
            Vector2 diff = v1 - v2;
            float d_x = Math.Abs(diff.x);
            float d_y = Math.Abs(diff.y);
            if(d_x > d_y) return d_x;
            else return d_y;
        }

        public static ulong GetRandomUID()
        {
            Span<byte> bytes = stackalloc byte[8];
            RandomNumberGenerator.Fill(bytes);
            return BitConverter.ToUInt64(bytes);
        }
    }
}
