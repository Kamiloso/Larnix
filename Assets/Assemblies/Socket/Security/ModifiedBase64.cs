#nullable enable
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Larnix.Socket.Security;

internal static class ModifiedBase64
{
    private const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789#&";

    public static int ToIndex(char c)
    {
        int index = CHARS.IndexOf(c);
        if (index == -1)
            throw new ArgumentException($"Character '{c}' is not a valid Base64 character.", nameof(c));
        return index;
    }

    public static char ToChar(int index)
    {
        if (index < 0 || index >= CHARS.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        return CHARS[index];
    }

    public static string Encode(byte[] bytes, int strLength)
    {
        while (bytes.Length % 3 != 0)
            bytes = bytes.Concat(new byte[] { 0 }).ToArray();

        StringBuilder sb = new();
        for (int i = 0; i < bytes.Length; i += 3)
        {
            char c1 = ToChar((bytes[i + 0] & 0b11111100) >> 2);
            char c2 = ToChar((bytes[i + 0] & 0b00000011) << 4 | (bytes[i + 1] & 0b11110000) >> 4);
            char c3 = ToChar((bytes[i + 1] & 0b00001111) << 2 | (bytes[i + 2] & 0b11000000) >> 6);
            char c4 = ToChar((bytes[i + 2] & 0b00111111) >> 0);

            sb.Append("" + c1 + c2 + c3 + c4);
        }

        while (sb.Length < strLength)
            sb.Append(CHARS[0]);

        return sb.ToString()[..strLength];
    }

    public static byte[] Decode(string str, int byteLength)
    {
        while (str.Length % 4 != 0)
            str += CHARS[0];

        List<byte> list = new();
        for (int i = 0; i < str.Length; i += 4)
        {
            int n1 = ToIndex(str[i + 0]);
            int n2 = ToIndex(str[i + 1]);
            int n3 = ToIndex(str[i + 2]);
            int n4 = ToIndex(str[i + 3]);

            list.Add((byte)(n1 << 2 | n2 >> 4));
            list.Add((byte)(n2 << 4 | n3 >> 2));
            list.Add((byte)(n3 << 6 | n4 >> 0));
        }

        while (list.Count < byteLength)
            list.Add(0);

        return list.Take(byteLength).ToArray();
    }
}
