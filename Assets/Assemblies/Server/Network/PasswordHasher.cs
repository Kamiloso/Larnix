#nullable enable
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Larnix.Core.Utils;
using Larnix.Socket.Server.Interfaces;

namespace Larnix.Server.Network;

internal class PasswordHasher : IPasswordHasher
{
    private static int CacheLimit => 256;

    private readonly ConcurrentDictionary<string, byte[]> _cache = new();

    public string HashPassword(string password)
    {
        byte[] salt = RandUtils.SecureBytes(16);
        byte[] hash = HashString(password, salt);

        return MergeSaltAndHash(salt, hash);
    }

    public bool VerifyPassword(string password, string storedSaltedHash)
    {
        if (SplitSaltedHash(storedSaltedHash, out byte[] salt, out byte[] storedHash))
        {
            byte[] hash = HashString(password, salt);
            return CryptographicOperations.FixedTimeEquals(storedHash, hash);
        }
        return false;
    }

    private byte[] HashString(string str, byte[] salt)
    {
        string ihs = InputHashingString(str, salt);

        if (_cache.TryGetValue(ihs, out byte[] cached))
        {
            return cached;
        }

        using var pbkdf2 = new Rfc2898DeriveBytes(str, salt, 100_000, HashAlgorithmName.SHA256);
        
        byte[] hash = pbkdf2.GetBytes(32);

        if (_cache.Count > CacheLimit)
        {
            _cache.Clear();
        }

        _cache.TryAdd(ihs, hash);

        return hash;
    }

    private bool SplitSaltedHash(string storedSaltedHash, out byte[] salt, out byte[] storedHash)
    {
        string[] parts = storedSaltedHash.Split(':');
        if (parts.Length != 2)
        {
            salt = null!;
            storedHash = null!;
            return false;
        }

        salt = Convert.FromBase64String(parts[0]);
        storedHash = Convert.FromBase64String(parts[1]);

        return true;
    }

    private string MergeSaltAndHash(byte[] salt, byte[] hash)
    {
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private string InputHashingString(string str, byte[] salt)
    {
        return str + '\0' + Convert.ToBase64String(salt);
    }
}
