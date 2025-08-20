using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using Org.BouncyCastle.Crypto.Parameters;
using Larnix.Files;
using System.Text;
using System.Diagnostics;

namespace Larnix.Server.Data
{
    public static class KeyObtainer
    {
        private const string filename = "rsa_keypair.pem";
        private const string secretfile = "server_secret.txt";

        public static RSA ObtainKeyRSA(string path)
        {
            AsymmetricCipherKeyPair keyPair = null;
            string data = null;

            data = FileManager.Read(path, filename);
            if (data != null)
            {
                keyPair = ParseRSA(data);
                if(keyPair != null)
                {
                    return BouncyToRSA(keyPair);
                }
            }

            var keyGen = new RsaKeyPairGenerator();
            keyGen.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
            keyPair = keyGen.GenerateKeyPair();

            data = ConvertKeyPairToPem(keyPair);
            FileManager.Write(path, filename, data);
            return BouncyToRSA(keyPair);
        }

        public static long ObtainSecret(string path)
        {
            string data = FileManager.Read(path, secretfile);
            if (data != null)
            {
                if (long.TryParse(data, out long readSecret))
                    return readSecret;
            }

            long secret = Common.GetSecureLong();
            FileManager.Write(path, secretfile, secret.ToString());
            return secret;
        }

        private static AsymmetricCipherKeyPair ParseRSA(string text)
        {
            try
            {
                using (var reader = new StringReader(text))
                {
                    var pemReader = new PemReader(reader);
                    var obj = pemReader.ReadObject();

                    if (obj is AsymmetricCipherKeyPair keyPair)
                        return keyPair;
                    else
                        return null;
                }
            }
            catch { return null; }
        }

        private static string ConvertKeyPairToPem(AsymmetricCipherKeyPair keyPair)
        {
            using (var stringWriter = new StringWriter())
            {
                var pemWriter = new PemWriter(stringWriter);
                pemWriter.WriteObject(keyPair);
                pemWriter.Writer.Flush();
                return stringWriter.ToString();
            }
        }

        private static RSA BouncyToRSA(AsymmetricCipherKeyPair keyPair)
        {
            var privateKeyParams = (RsaPrivateCrtKeyParameters)keyPair.Private;

            var rsaParams = new RSAParameters
            {
                Modulus = privateKeyParams.Modulus.ToByteArrayUnsigned(),
                Exponent = privateKeyParams.PublicExponent.ToByteArrayUnsigned(),
                D = privateKeyParams.Exponent.ToByteArrayUnsigned(),
                P = privateKeyParams.P.ToByteArrayUnsigned(),
                Q = privateKeyParams.Q.ToByteArrayUnsigned(),
                DP = privateKeyParams.DP.ToByteArrayUnsigned(),
                DQ = privateKeyParams.DQ.ToByteArrayUnsigned(),
                InverseQ = privateKeyParams.QInv.ToByteArrayUnsigned()
            };

            var rsa = RSA.Create();
            rsa.ImportParameters(rsaParams);
            return rsa;
        }

        const string Base64 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#&";
        private const int VERIFY_PART_LENGTH = 12;
        private const int SECRET_PART_LENGTH = 11; // must be at least 11, must fit one long
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
