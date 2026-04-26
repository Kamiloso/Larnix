#nullable enable
using Larnix.Core.Files;
using Org.BouncyCastle.Crypto.Generators;
using System.Text;
using Larnix.Core.Utils;
using Larnix.Core.Serialization;
using System.Linq;

namespace Larnix.Socket.Security;

public static class Authcode
{
    private const string AUTH_BASE_64 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#&";

    private const int VERIFY_LNGT = 12;
    private const int SECRET_LNGT = 11; // must be at least 11 to fit one long
    private const int TOTAL_LNGT = VERIFY_LNGT + SECRET_LNGT + 1; // +1 for checksum
    private const int SEGMENT_SIZE = 6;

    internal static long ObtainSecret(string path, string filename)
    {
        string? data = FileManager.Read(path, filename);
        if (data != null)
        {
            if (long.TryParse(data, out long readSecret))
                return readSecret;
        }

        long secret = RandUtils.SecureLong();
        FileManager.Write(path, filename, secret.ToString());
        return secret;
    }

    internal static string ProduceAuthCodeRSA(byte[] key, long secret)
    {
        string raw = ProduceRawAuthCodeRSA(key, secret);
        return InsertDashes(raw, SEGMENT_SIZE);
    }

    internal static string ProduceRawAuthCodeRSA(byte[] key, long secret)
    {
        byte[] hash = DeriveKeyScrypt(key, Binary<long>.Serialize(-7264111368357934733L)); // random, hard-coded salt

        StringBuilder sb = new();
        for (int i = 0; i < VERIFY_LNGT; i++)
        {
            sb.Append(AUTH_BASE_64[hash[i] % 64]);
        }

        ulong usecret = (ulong)secret;
        while (sb.Length < VERIFY_LNGT + SECRET_LNGT)
        {
            int mod = (int)(usecret % 64);
            usecret /= 64;
            sb.Insert(VERIFY_LNGT, AUTH_BASE_64[mod]);
        }

        int checksum = 0;
        foreach (char c in sb.ToString())
        {
            checksum += c;
        }
        sb.Append(AUTH_BASE_64[checksum % 64]);

        return sb.ToString();
    }

    public static bool IsGoodAuthcode(string authCodeRSA)
    {
        string code = authCodeRSA.Replace("-", "");
        if (InsertDashes(code, SEGMENT_SIZE) != authCodeRSA)
        {
            return false;
        }

        if (code.Length != TOTAL_LNGT)
        {
            return false;
        }

        if (code.Any(c => !AUTH_BASE_64.Contains(c)))
        {
            return false;
        }

        int checksum = 0;
        for (int i = 0; i < TOTAL_LNGT - 1; i++)
        {
            checksum += code[i];
        }

        return AUTH_BASE_64[checksum % 64] == code[TOTAL_LNGT - 1];
    }

    internal static bool VerifyPublicKey(byte[] key, string authCodeRSA)
    {
        string code1 = authCodeRSA.Replace("-", "")[..VERIFY_LNGT];
        string code2 = ProduceRawAuthCodeRSA(key, 0)[..VERIFY_LNGT];

        return code1 == code2;
    }

    internal static long GetSecretFromAuthCode(string authCodeRSA)
    {
        string code1 = authCodeRSA.Replace("-", "").Substring(VERIFY_LNGT, SECRET_LNGT);

        ulong usecret = 0;
        for (int i = 0; i < SECRET_LNGT; i++)
        {
            usecret *= 64;
            usecret += (ulong)AUTH_BASE_64.IndexOf(code1[i]);
        }

        return (long)usecret;
    }

    private static string InsertDashes(string input, int n)
    {
        if (string.IsNullOrEmpty(input) || n <= 0)
            return input;

        StringBuilder sb = new(input.Length + input.Length / n);
        for (int i = 0; i < input.Length; i++)
        {
            if (i > 0 && i % n == 0)
                sb.Append('-');
            sb.Append(input[i]);
        }
        return sb.ToString();
    }

    private static byte[] DeriveKeyScrypt(byte[] password, byte[] salt)
    {
        return SCrypt.Generate(password, salt,
            N: 1 << 14, // 16 MB
            r: 8,
            p: 1,
            dkLen: VERIFY_LNGT);
    }
}
