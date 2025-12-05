using System;
using System.IO;
using System.Security.Cryptography;
using Larnix.Core.Utils;

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

            byte[] encrypted = new byte[plaintext.Length];
            byte[] tag = new byte[TagSize];

            using (var aes = new AesGcm(_key))
            {
                aes.Encrypt(nonce, plaintext, encrypted, tag);
            }

            return ArrayUtils.MegaConcat(nonce, tag, encrypted);
        }

        public byte[] Decrypt(byte[] ciphertext)
        {
            if (ciphertext == null)
                throw new ArgumentNullException(nameof(ciphertext));

            if (ciphertext.Length < NonceSize + TagSize)
            {
                return new byte[0];
            }

            ReadOnlySpan<byte> fullData = new ReadOnlySpan<byte>(ciphertext);

            ReadOnlySpan<byte> nonce = fullData.Slice(0, NonceSize);
            ReadOnlySpan<byte> tag = fullData.Slice(NonceSize, TagSize);
            ReadOnlySpan<byte> encrypted = fullData.Slice(NonceSize + TagSize);

            byte[] plaintext = new byte[encrypted.Length];

            try
            {
                using (var aes = new AesGcm(_key))
                {
                    aes.Decrypt(nonce, encrypted, tag, plaintext);
                }

                return plaintext;
            }
            catch (CryptographicException)
            {
                return new byte[0];
            }
        }
    }
}
