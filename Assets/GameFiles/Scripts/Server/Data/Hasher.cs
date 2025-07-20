using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public static class Hasher
{
    public static string HashPassword(string password)
    {
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256))
        {
            byte[] hash = pbkdf2.GetBytes(32);
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }
    }

    public static bool VerifyPassword(string password, string storedSaltedHash)
    {
        string[] parts = storedSaltedHash.Split(':');
        if (parts.Length != 2)
            return false;

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] storedHash = Convert.FromBase64String(parts[1]);

        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256))
        {
            byte[] hash = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(storedHash, hash);
        }
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
