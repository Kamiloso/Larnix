using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Larnix.Socket.Security
{
    internal static class Encryption
    {
        internal static byte[] EncryptAES(byte[] bytes, byte[] key)
        {
            if (key.Length != 16)
                throw new Exception("AES key length must be 16 bytes.");

            if(bytes == null)
                bytes = new byte[0];

            byte[] iv = new byte[16];
            using(var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using MemoryStream ms = new();
            using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(bytes, 0, bytes.Length);
            }

            byte[] encrypted = ms.ToArray();
            return iv.Concat(encrypted).ToArray();
        }

        internal static byte[] DecryptAES(byte[] bytes, byte[] key)
        {
            if (key == null || key.Length != 16)
                throw new Exception("AES key length must be 16 bytes.");

            if (bytes == null || bytes.Length < 16)
                return new byte[0];

            byte[] iv = bytes[0..16];
            byte[] encryptedBytes = bytes[16..bytes.Length];

            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;

            try
            {
                using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using MemoryStream ms = new(encryptedBytes);
                using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
                using MemoryStream resultStream = new();

                cs.CopyTo(resultStream);
                return resultStream.ToArray();
            }
            catch
            {
                return new byte[0];
            }
        }

        internal static byte[] EncryptRSA(byte[] data, RSA rsa)
        {
            try
            {
                return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
            }
            catch
            {
                return new byte[0];
            }
        }

        internal static byte[] DecryptRSA(byte[] data, RSA rsa)
        {
            try
            {
                return rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA1);
            }
            catch
            {
                return new byte[0];
            }
        }
    }
}
