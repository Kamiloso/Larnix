#nullable enable
using Larnix.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Larnix.Model;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Version
{
    public static readonly Version Current = new("0.0.47");

    public readonly uint Value;

    public Version(uint value)
    {
        Value = value;
    }

    /// <summary>
    /// Examples: "1", "1.2", "1.2.3", "1.2.3.4". Fourth number doesn't affect compatibility.
    /// </summary>
    public Version(string str)
    {
        try
        {
            List<byte> segments = str
                .Split('.')
                .Select(s => byte.Parse(s))
                .ToList();

            if (segments.Count > 4)
                throw new Exception();

            while (segments.Count < 4)
                segments.Add(0);

            uint constructID = 0;
            foreach (byte b in segments)
            {
                constructID <<= 8;
                constructID |= b;
            }
            Value = constructID;
        }
        catch
        {
            throw new ArgumentException($"Version {str} is invalid!");
        }
    }

    public bool CompatibleWith(Version version)
    {
        return Value >> 8 == version.Value >> 8;
    }

    public override string ToString()
    {
        List<byte> segments = new()
        {
            (byte)((0xFF_00_00_00 & Value) >> 24),
            (byte)((0x00_FF_00_00 & Value) >> 16),
            (byte)((0x00_00_FF_00 & Value) >> 8),
            (byte)((0x00_00_00_FF & Value) >> 0),
        };

        while (segments.Count > 2 && segments[segments.Count - 1] == 0)
            segments.RemoveAt(segments.Count - 1);

        return string.Join(".", segments);
    }

    public static bool operator <(Version a, Version b) => a.Value < b.Value;
    public static bool operator >(Version a, Version b) => a.Value > b.Value;
    public static bool operator <=(Version a, Version b) => a.Value <= b.Value;
    public static bool operator >=(Version a, Version b) => a.Value >= b.Value;
}
