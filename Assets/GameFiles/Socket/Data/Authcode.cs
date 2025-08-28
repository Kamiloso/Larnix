using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Larnix.Socket.Data
{
    public static class Authcode
    {
        const string Base64 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#&";
        private const int VERIFY_PART_LENGTH = 12;
        private const int SECRET_PART_LENGTH = 11; // must be at least 11 to fit one long
        private const int TOTAL_LENGTH = VERIFY_PART_LENGTH + SECRET_PART_LENGTH + 1 /* +checksum */;
        private const int SEGMENT_SIZE = 6;

        public static string ProduceAuthCodeRSA(byte[] key, long secret)
        {
            string raw = ProduceRawAuthCodeRSA(key, secret);
            return Common.InsertDashes(raw, SEGMENT_SIZE);
        }

        private static string ProduceRawAuthCodeRSA(byte[] key, long secret)
        {
            const int ITERATIONS = 50_000;
            byte[] hash = key;

            using (var incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                for (int i = 0; i < ITERATIONS; i++)
                {
                    incrementalHash.AppendData(hash);
                    hash = incrementalHash.GetHashAndReset();
                }
            }

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
            if (Common.InsertDashes(code, SEGMENT_SIZE) != authCodeRSA)
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
    }
}
