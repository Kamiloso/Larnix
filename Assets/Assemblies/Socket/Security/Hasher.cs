using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Larnix.Core.Utils;

namespace Larnix.Socket.Security
{
    internal static class Hasher
    {
        private const int MAX_CACHE_COUNT = 256;

        private static Dictionary<string, byte[]> _hashingCache = new();
        private static object _lock = new();

        private static string InputHashingString(string str, byte[] salt)
        {
            return str + '\0' + Convert.ToBase64String(salt);
        }

        private static string MergeSaltAndHash(byte[] salt, byte[] hash)
        {
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        private static byte[] HashString(string str, byte[] salt)
        {
            string ihs = InputHashingString(str, salt);

            lock (_lock)
            {
                if (_hashingCache.TryGetValue(ihs, out var cached))
                    return cached;
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(str, salt, 100_000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);

                lock (_lock)
                {
                    if (_hashingCache.Count > MAX_CACHE_COUNT)
                        _hashingCache.Clear();

                    _hashingCache[ihs] = hash;
                }

                return hash;
            }
        }

        private static bool SplitSaltedHash(string storedSaltedHash, out byte[] salt, out byte[] storedHash)
        {
            string[] parts = storedSaltedHash.Split(':');
            if (parts.Length != 2)
            {
                salt = null;
                storedHash = null;
                return false;
            }

            salt = Convert.FromBase64String(parts[0]);
            storedHash = Convert.FromBase64String(parts[1]);
            return true;
        }

        public static bool InCache(string password, string storedSaltedHash, out bool result)
        {
            if (!SplitSaltedHash(storedSaltedHash, out byte[] salt, out _))
            {
                result = default;
                return false;
            }

            string ihs = InputHashingString(password, salt);

            lock (_lock)
            {
                if (_hashingCache.TryGetValue(ihs, out var hash))
                {
                    result = VerifyPassword(password, storedSaltedHash);
                    return true;
                }
            }

            result = default;
            return false;
        }

        public static string HashPassword(string password)
        {
            byte[] salt = Common.GetSecureBytes(16);
            byte[] hash = HashString(password, salt);
            return MergeSaltAndHash(salt, hash);
        }

        public static bool VerifyPassword(string password, string storedSaltedHash)
        {
            if (!SplitSaltedHash(storedSaltedHash, out byte[] salt, out byte[] storedHash))
                return false;

            byte[] hash = HashString(password, salt);
            return CryptographicOperations.FixedTimeEquals(storedHash, hash);
        }

        public static async Task<string> HashPasswordAsync(string password)
        {
            return await Task.Run(() => HashPassword(password));
        }

        public static async Task<bool> VerifyPasswordAsync(string password, string storedSaltedHash)
        {
            return await Task.Run(() => VerifyPassword(password, storedSaltedHash));
        }
    }
}
