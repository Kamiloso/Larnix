using System;
using Larnix.Core.Utils;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Larnix.Socket.Security.Keys;

internal class KeyAES : IEncryptionKey
{
    private const int KEY_SIZE = 32;
    private const int NONCE_SIZE = 12;
    private const int TAG_SIZE = 16;

    private readonly byte[] _key;

    public KeyAES(byte[] keyBytes)
    {
        if (keyBytes.Length != KEY_SIZE)
            throw new ArgumentException($"AES key length must be {KEY_SIZE} bytes (256-bit)!", nameof(keyBytes));

        _key = keyBytes;
    }

    public static KeyAES GenerateNew()
    {
        byte[] bytes = RandUtils.SecureBytes(KEY_SIZE);
        return new KeyAES(bytes);
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

        byte[] nonce = RandUtils.SecureBytes(NONCE_SIZE);

        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(
            new KeyParameter(_key),
            TAG_SIZE * 8,
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

        if (ciphertext.Length < NONCE_SIZE + TAG_SIZE)
            return new byte[0];

        byte[] nonce = new byte[NONCE_SIZE];
        byte[] encrypted = new byte[ciphertext.Length - NONCE_SIZE];

        Array.Copy(ciphertext, 0, nonce, 0, NONCE_SIZE);
        Array.Copy(ciphertext, NONCE_SIZE, encrypted, 0, encrypted.Length);

        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(
            new KeyParameter(_key),
            TAG_SIZE * 8,
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
