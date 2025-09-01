using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace QuickNet.Processing
{
    public static class Hasher
    {
        private static Dictionary<string, byte[]> HashingCache = new();
        private const int MAX_CACHE_COUNT = 256;
        private static object locker = new();

        private static string InputHashingString(string str, byte[] salt)
        {
            return str + '\0' + Convert.ToBase64String(salt);
        }

        private static byte[] HashString(string str, byte[] salt)
        {
            string ihs = InputHashingString(str, salt);

            lock (locker)
            {
                if (HashingCache.TryGetValue(ihs, out var cached))
                    return cached;
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(str, salt, 100_000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);

                lock (locker)
                {
                    if (HashingCache.Count > MAX_CACHE_COUNT)
                        HashingCache.Clear();

                    HashingCache[ihs] = hash;
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

        public static bool InCache(string password, string storedSaltedHash)
        {
            if (!SplitSaltedHash(storedSaltedHash, out byte[] salt, out byte[] storedHash))
                return false;

            string ihs = InputHashingString(password, salt);

            lock (locker)
            {
                return HashingCache.ContainsKey(ihs);
            }
        }

        public static string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = HashString(password, salt);
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedSaltedHash)
        {
            if (!SplitSaltedHash(storedSaltedHash, out byte[] salt, out byte[] storedHash))
                return false;

            byte[] hash = HashString(password, salt);
            return CryptographicOperations.FixedTimeEquals(storedHash, hash);
        }

        public static Task<string> HashPasswordAsync(string password)
        {
            return Task.Run(() => HashPassword(password));
        }

        public static Task<bool> VerifyPasswordAsync(string password, string storedSaltedHash)
        {
            return Task.Run(() => VerifyPassword(password, storedSaltedHash));
        }
    }
}
