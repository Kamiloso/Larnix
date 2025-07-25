using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace Larnix.Socket
{
    public static class Encryption
    {
        public class Settings
        {
            public enum Type : byte { AES, RSA };
            public Type type { get; set; }
            public byte[] key { get; set; }
            public RSA rsa { get; set; }

            public Settings(Type _type, byte[] _key)
            {
                type = _type;
                key = _key;

                if (type == Type.AES)
                {
                    if (key.Length != 16)
                        throw new Exception("AES key must have length 16.");
                }
                else throw new Exception("Wrong constructor was used to create Encryption.Settings class.");
            }

            public Settings(Type _type, RSA _rsa)
            {
                type = _type;
                rsa = _rsa;

                if (type != Type.RSA)
                    throw new Exception("Wrong constructor was used to create Encryption.Settings class.");
            }

            public byte[] Encrypt(byte[] bytes)
            {
                switch(type)
                {
                    case Type.AES: return EncryptAES(bytes, key);
                    case Type.RSA: return EncryptRSA(bytes, rsa);
                    default: throw new Exception("Not implemented encryption.");
                }
            }

            public byte[] Decrypt(byte[] bytes)
            {
                switch (type)
                {
                    case Type.AES: return DecryptAES(bytes, key);
                    case Type.RSA: return DecryptRSA(bytes, rsa);
                    default: throw new Exception("Not implemented decryption.");
                }
            }
        }

        static byte[] EncryptAES(byte[] bytes, byte[] key)
        {
            if (key.Length != 16)
                throw new System.Exception("AES key length must be 16 bytes.");

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

        static byte[] DecryptAES(byte[] bytes, byte[] key)
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
            catch (Exception)
            {
                return new byte[0];
            }
        }

        static byte[] EncryptRSA(byte[] data, RSA rsa)
        {
            try
            {
                return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }

        static byte[] DecryptRSA(byte[] data, RSA rsa)
        {
            try
            {
                return rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA1);
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }
    }
}
