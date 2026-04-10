#nullable enable
using Larnix.Core.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Larnix.Core.Utils;

public static class FixedStringUtils
{
    public static T[] Cut<T>(string str, Func<string, T> constructor) where T : unmanaged, IFixedString
    {
        int capacity = new T().Capacity;

        List<T> parts = new();
        for (int i = 0; i < str.Length; i += capacity)
        {
            string part = str[i..Math.Min(i + capacity, str.Length)];
            parts.Add(constructor(part));
        }

        if (parts.Count == 0)
        {
            parts.Add(constructor(""));
        }

        return parts.ToArray();
    }

    public static string Join<T>(T[] parts) where T : unmanaged, IFixedString
    {
        StringBuilder sb = new();
        foreach (T part in parts)
        {
            string str = part.ToString();
            sb.Append(str);
        }
        return sb.ToString();
    }
}
