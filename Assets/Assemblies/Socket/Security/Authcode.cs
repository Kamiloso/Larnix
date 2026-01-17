using Larnix.Core.Files;
using Larnix.Core.Utils;
using System.Collections;
using System.Collections.Generic;
using Org.BouncyCastle.Crypto.Generators;
using System.Text;
using Larnix.Core;

namespace Larnix.Socket.Security
{
    internal static class Authcode
    {
        private const string Base64 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#&";

        private const int VERIFY_PART_LENGTH = 12;
        private const int SECRET_PART_LENGTH = 11; // must be at least 11 to fit one long
        private const int TOTAL_LENGTH = VERIFY_PART_LENGTH + SECRET_PART_LENGTH + 1 /* +checksum */;
        private const int SEGMENT_SIZE = 6;

        public static long ObtainSecret(string path, string filename)
        {
            string data = FileManager.Read(path, filename);
            if (data != null)
            {
                if (long.TryParse(data, out long readSecret))
                    return readSecret;
            }

            long secret = Common.GetSecureLong();
            FileManager.Write(path, filename, secret.ToString());
            return secret;
        }

        public static string ProduceAuthCodeRSA(byte[] key, long secret)
        {
            string raw = ProduceRawAuthCodeRSA(key, secret);
            return InsertDashes(raw, SEGMENT_SIZE);
        }

        public static string ProduceRawAuthCodeRSA(byte[] key, long secret)
        {
            byte[] hash = DeriveKeyScrypt(key, EndianUnsafe.GetBytes((long)-7264111368357934733)); // random, hard-coded salt

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < VERIFY_PART_LENGTH; i++)
                sb.Append(Base64[hash[i] % 64]);

            ulong usecret = (ulong)secret;
            while (sb.Length < VERIFY_PART_LENGTH + SECRET_PART_LENGTH)
            {
                int mod = (int)(usecret % 64);
                usecret /= 64;
                sb.Insert(VERIFY_PART_LENGTH, Base64[mod]);
            }

            int checksum = 0;
            foreach (char c in sb.ToString())
            {
                checksum += c;
            }
            sb.Append(Base64[checksum % 64]);

            return sb.ToString();
        }

        public static bool IsGoodAuthcode(string authCodeRSA)
        {
            if (authCodeRSA == null)
                return false;

            string code = authCodeRSA.Replace("-", "");
            if (InsertDashes(code, SEGMENT_SIZE) != authCodeRSA)
                return false;

            if (code.Length != TOTAL_LENGTH)
                return false;

            foreach (char c in code)
            {
                if (!Base64.Contains(c))
                    return false;
            }

            int checksum = 0;
            for (int i = 0; i < TOTAL_LENGTH - 1; i++)
            {
                checksum += code[i];
            }

            return Base64[checksum % 64] == code[TOTAL_LENGTH - 1];
        }

        public static bool VerifyPublicKey(byte[] key, string authCodeRSA)
        {
            string code1 = authCodeRSA.Replace("-", "").Substring(0, VERIFY_PART_LENGTH);
            string code2 = ProduceRawAuthCodeRSA(key, 0).Substring(0, VERIFY_PART_LENGTH);

            return code1 == code2;
        }

        public static long GetSecretFromAuthCode(string authCodeRSA)
        {
            string code1 = authCodeRSA.Replace("-", "").Substring(VERIFY_PART_LENGTH, SECRET_PART_LENGTH);

            ulong usecret = 0;
            for (int i = 0; i < SECRET_PART_LENGTH; i++)
            {
                usecret *= 64;
                usecret += (ulong)Base64.IndexOf(code1[i]);
            }

            return (long)usecret;
        }

        private static string InsertDashes(string input, int n)
        {
            if (string.IsNullOrEmpty(input) || n <= 0)
                return input;

            StringBuilder sb = new StringBuilder(input.Length + input.Length / n);
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
                dkLen: VERIFY_PART_LENGTH);
        }
    }
}
