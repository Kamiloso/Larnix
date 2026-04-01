#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Larnix.Core.Binary;

namespace Larnix.Core.Utils;

public static class RandUtils
{
    private static readonly ThreadLocal<Random> _random = new(() => new());
    public static Random Rand => _random.Value;

    public static int GetInt(int max) => Rand.Next(max);
    public static int GetInt(int min, int max) => Rand.Next(min, max);
    public static double GetDouble() => Rand.NextDouble();

    public static int NextInt()
    {
        Span<byte> buffer = stackalloc byte[4];
        Rand.NextBytes(buffer);
        return BitConverter.ToInt32(buffer);
    }

    public static long NextLong()
    {
        Span<byte> buffer = stackalloc byte[8];
        Rand.NextBytes(buffer);
        return BitConverter.ToInt64(buffer);
    }

    public static bool NextBool()
    {
        return (NextInt() & 1) == 1;
    }

    public static byte[] NextBytes(int size)
    {
        var buffer = new byte[size];
        Rand.NextBytes(buffer);
        return buffer;
    }

    public static int SecureInt()
    {
        Span<byte> buffer = stackalloc byte[4];
        RandomNumberGenerator.Fill(buffer);
        return BitConverter.ToInt32(buffer);
    }

    public static long SecureLong()
    {
        Span<byte> buffer = stackalloc byte[8];
        RandomNumberGenerator.Fill(buffer);
        return BitConverter.ToInt64(buffer);
    }

    public static bool SecureBool()
    {
        return (SecureInt() & 1) == 1;
    }

    public static byte[] SecureBytes(int size)
    {
        var buffer = new byte[size];
        RandomNumberGenerator.Fill(buffer);
        return buffer;
    }

    public static long SeedFromString(string input)
    {
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Primitives.FromBytes<long>(hash, 0);
    }
}
