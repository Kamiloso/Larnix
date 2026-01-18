using System.Collections;
using System.Collections.Generic;
using System;
using System.Security.Cryptography;
using Larnix.Core.Files;
using Larnix.Core;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System.IO;
using Larnix.Core.Utils;

namespace Larnix.Socket.Security.Keys
{
    public class KeyRSA : IEncryptionKey, IDisposable
    {
        private const int PublicKeySize = 264;
        private readonly bool _isFullKey;
        private readonly RSA _rsa;

        private readonly bool _isBroken;

        private bool _disposed;

        public KeyRSA(string path, string filename) // private key
        {
            AsymmetricCipherKeyPair keyPair;

            string data = FileManager.Read(path, filename);
            if (data == null || (keyPair = ParseRSA(data)) == null)
            {
                // generate key
                var keyGen = new RsaKeyPairGenerator();
                keyGen.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
                keyPair = keyGen.GenerateKeyPair();

                data = ConvertKeyPairToPem(keyPair);
                FileManager.Write(path, filename, data);
            }

            _isFullKey = true;
            _rsa = BouncyToRSA(keyPair);
        }

        public KeyRSA(byte[] keyBytes) // public key
        {
            if (keyBytes == null)
                throw new ArgumentNullException(nameof(keyBytes));

            _isFullKey = false;
            _rsa = RSA.Create();

            try
            {
                _rsa.ImportParameters(new RSAParameters
                {
                    Modulus = keyBytes[..256],
                    Exponent = ArrayUtils.RemoveLeadingZeros(keyBytes[256..])
                });
            }
            catch
            {
                _isBroken = true;
            }
        }

        public byte[] ExportPublicKey()
        {
            if (_isBroken)
                return new byte[PublicKeySize];

            var parameters = _rsa.ExportParameters(false);

            return ArrayUtils.MegaConcat(
                parameters.Modulus,
                ArrayUtils.AddLeadingZeros(parameters.Exponent, 8)
                );
        }

        public byte[] Encrypt(byte[] plaintext)
        {
            return !_isBroken ?
                _rsa.Encrypt(plaintext, RSAEncryptionPadding.OaepSHA1) :
                new byte[0];
        }

        public byte[] Decrypt(byte[] ciphertext)
        {
            if (!_isFullKey)
                throw new InvalidOperationException("Cannot decrypt using public key!");

            try
            {
                return !_isBroken ?
                    _rsa.Decrypt(ciphertext, RSAEncryptionPadding.OaepSHA1) :
                    new byte[0];
            }
            catch (CryptographicException)
            {
                return new byte[0];
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _rsa.Dispose();
            }
        }

        // -------------------------- //
        // ===== STATIC METHODS ===== //
        // -------------------------- //

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
            catch
            {
                return null;
            }
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
    }
}
