using System;
using System.IO;
using System.Security.Cryptography;
using Larnix.Core.Utils;
using Larnix.Core;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Larnix.Socket.Security.Keys
{
    internal class KeyAES : IEncryptionKey
    {
        private const int KeySize = 32;
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private readonly byte[] _key;

        public KeyAES(byte[] keyBytes = null)
        {
            if (keyBytes != null)
            {
                if (keyBytes.Length != KeySize)
                    throw new ArgumentException($"AES key length must be {KeySize} bytes (256-bit)!", nameof(keyBytes));

                _key = keyBytes;
            }
            else
            {
                _key = Common.GetSecureBytes(KeySize);
            }
        }

        public byte[] ExportKey()
        {
            byte[] exported = new byte[_key.Length];
            Array.Copy(_key, exported, _key.Length);
            return exported;
        }

        public byte[] Encrypt(byte[] plaintext)
        {
            if (plaintext == null)
                throw new ArgumentNullException(nameof(plaintext));

            byte[] nonce = Common.GetSecureBytes(NonceSize);

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(
                new KeyParameter(_key),
                TagSize * 8,
                nonce
            );

            cipher.Init(true, parameters);

            byte[] output = new byte[cipher.GetOutputSize(plaintext.Length)];
            int len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, output, 0);
            cipher.DoFinal(output, len);

            return ArrayUtils.MegaConcat(nonce, output);
        }

        public byte[] Decrypt(byte[] ciphertext)
        {
            if (ciphertext == null)
                throw new ArgumentNullException(nameof(ciphertext));

            if (ciphertext.Length < NonceSize + TagSize)
                return new byte[0];

            byte[] nonce = new byte[NonceSize];
            byte[] encrypted = new byte[ciphertext.Length - NonceSize];

            Array.Copy(ciphertext, 0, nonce, 0, NonceSize);
            Array.Copy(ciphertext, NonceSize, encrypted, 0, encrypted.Length);

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(
                new KeyParameter(_key),
                TagSize * 8,
                nonce
            );

            cipher.Init(false, parameters);

            try
            {
                byte[] output = new byte[cipher.GetOutputSize(encrypted.Length)];
                int len = cipher.ProcessBytes(encrypted, 0, encrypted.Length, output, 0);
                cipher.DoFinal(output, len);

                return output;
            }
            catch
            {
                return new byte[0];
            }
        }
    }
}
