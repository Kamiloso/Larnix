using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Linq;

namespace Socket.Processing
{
    internal static class KeyObtainer
    {
        private const string filename = "rsa_keypair.pem";
        private const string secretfile = "server_secret.txt";

        internal static RSA ObtainKeyRSA(string path)
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

        internal static long ObtainSecret(string path)
        {
            string data = FileManager.Read(path, secretfile);
            if (data != null)
            {
                if (long.TryParse(data, out long readSecret))
                    return readSecret;
            }

            long secret = GetSecureLong();
            FileManager.Write(path, secretfile, secret.ToString());
            return secret;
        }

        internal static byte[] KeyToPublicBytes(RSA rsa)
        {
            if (rsa == null)
                return null;

            RSAParameters publicKey = rsa.ExportParameters(false);
            byte[] modulus = publicKey.Modulus;
            byte[] exponent = publicKey.Exponent;

            if (modulus.Length > 256)
                modulus = modulus[0..256];

            while (exponent.Length < 8)
                exponent = (new byte[1]).Concat(exponent).ToArray();

            if (exponent.Length > 8)
                exponent = exponent[0..8];

            return modulus.Concat(exponent).ToArray();
        }

        internal static RSA PublicBytesToKey(byte[] publicBytes)
        {
            try
            {
                RSA rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus = publicBytes[0..256],
                    Exponent = publicBytes[256..]
                });

                return rsa;
            }
            catch
            {
                return PublicBytesToKey(new byte[256 + 8]);
            }
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

        internal static long GetSecureLong()
        {
            var buffer = new byte[8];
            RandomNumberGenerator.Fill(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }
    }
}
