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

        public static RSA ObtainKeyRSA(string path, bool onlyLoad)
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

            if (onlyLoad)
                return null;

            var keyGen = new RsaKeyPairGenerator();
            keyGen.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
            keyPair = keyGen.GenerateKeyPair();

            data = ConvertKeyPairToPem(keyPair);
            FileManager.Write(path, filename, data);
            return BouncyToRSA(keyPair);
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

        public static string ProduceAuthCodeRSA(byte[] key)
        {
            // Example AuthCode: XXXX-XXXX-XXXX
            const string Base32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
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
            for (int i = 0; i < 3; i++)
            {
                if (i != 0)
                    sb.Append('-');

                for (int j = 0; j < 4; j++)
                    sb.Append(Base32[hash[4 * i + j] % 32]);
            }

            return sb.ToString();
        }

        public static bool VerifyPublicKey(byte[] key, string authCodeRSA)
        {
            return ProduceAuthCodeRSA(key) == authCodeRSA;
        }
    }
}
