#nullable enable
using Larnix.Socket.Security.Encryption;
using System;
using System.Security.Cryptography;

namespace Larnix.Socket.Security.Encryption;

internal class AesKey : IFullEncryptor
{
    private readonly byte[] _key;

    public AesKey(byte[] keyBytes)
    {
        _key = keyBytes[0..32];
    }

    public static AesKey Generate()
    {
        byte[] keyBytes = new byte[32];
        RandomNumberGenerator.Fill(keyBytes);
        return new AesKey(keyBytes);
    }

    public byte[] Encrypt(byte[] plaintext)
    {
        try
        {
            byte[] nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);

            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];

            using var aesGcm = new AesGcm(_key);
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

            byte[] result = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

            return result;
        }
        catch (Exception)
        {
            return Array.Empty<byte>();
        }
    }

    public byte[] Decrypt(byte[] ciphertext)
    {
        try
        {
            if (ciphertext.Length < 12 + 16)
            {
                throw new ArgumentException("Ciphertext is too short.");
            }

            ReadOnlySpan<byte> nonce = ciphertext.AsSpan(0, 12);
            ReadOnlySpan<byte> tag = ciphertext.AsSpan(12, 16);
            ReadOnlySpan<byte> actualCiphertext = ciphertext.AsSpan(28);

            byte[] plaintext = new byte[actualCiphertext.Length];

            using var aesGcm = new AesGcm(_key);
            aesGcm.Decrypt(nonce, actualCiphertext, tag, plaintext);

            return plaintext;
        }
        catch (Exception)
        {
            return Array.Empty<byte>();
        }
    }

    public byte[] Export()
    {
        return _key[..];
    }
}