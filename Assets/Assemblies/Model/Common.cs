#nullable enable
using Larnix.Core.Vectors;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Larnix.Model;

public static class Common
{
    public static string DatabaseFile => "database.sqlite";

    public static double ViewDistance => 50.0;
    public static double PhysicsSectorSize => 3.0;

    public static Vec2 WorldEpsilon => new(0.0001, 0.0001);
    public static Vec2 WorldEpsilonUp => new(0.0000, 0.0001);

    public static string SplitPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var step1 = Regex.Replace(input, @"([A-Z])(?=[A-Z][a-z])", "$1 ");
        return Regex.Replace(step1, @"(?<=[a-z])(?=[A-Z])", " ");
    }

    public static bool AreSameDirectory(string dir1, string dir2)
    {
        if (string.IsNullOrWhiteSpace(dir1) || string.IsNullOrWhiteSpace(dir2))
            return false;

        string full1 = Path.GetFullPath(dir1);
        string full2 = Path.GetFullPath(dir2);

        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? string.Equals(full1, full2, StringComparison.Ordinal)
            : string.Equals(full1, full2, StringComparison.OrdinalIgnoreCase);
    }
}
