#nullable enable
using System;
using Larnix.Core.Utils;
using Larnix.Socket.Payload.Structs;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Larnix.Socket.Security.Keys;

internal class KeyAes : IKey
{
    private const int KEY_SIZE = 32;
    private const int NONCE_SIZE = 12;
    private const int TAG_SIZE = 16;

    private readonly byte[] _key;

    private bool _disposed;

    private KeyAes(in FixedAes aesKey)
    {
        _key = aesKey.Bytes32();
    }

    public static KeyAes GenerateNew()
    {
        byte[] bytes = RandUtils.SecureBytes(KEY_SIZE);
        FixedAes aesKey = FixedAes.FromBytes(bytes);
        Array.Fill<byte>(bytes, 0);
        return new KeyAes(aesKey);
    }

    public static KeyAes FromStruct(in FixedAes aesKey)
    {
        return new KeyAes(aesKey);
    }

    public FixedAes ExportKey()
    {
        return FixedAes.FromBytes(_key);
    }

    public T CloneKey<T>() where T : IKey
    {
        FixedAes aesKey = ExportKey();
        return (T)(IKey)new KeyAes(aesKey);
    }

    public byte[] Encrypt(byte[] plaintext)
    {
        byte[] nonce = RandUtils.SecureBytes(NONCE_SIZE);

        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(
            key: new KeyParameter(_key),
            macSize: TAG_SIZE * 8,
            nonce: nonce
        );

        cipher.Init(true, parameters);

        byte[] output = new byte[cipher.GetOutputSize(plaintext.Length)];
        int len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, output, 0);
        cipher.DoFinal(output, len);

        return ArrayUtils.MegaConcat(nonce, output);
    }

    public byte[] Decrypt(byte[] ciphertext)
    {
        if (ciphertext.Length < NONCE_SIZE + TAG_SIZE)
            return Array.Empty<byte>();

        byte[] nonce = new byte[NONCE_SIZE];
        byte[] encrypted = new byte[ciphertext.Length - NONCE_SIZE];

        Array.Copy(ciphertext, 0, nonce, 0, NONCE_SIZE);
        Array.Copy(ciphertext, NONCE_SIZE, encrypted, 0, encrypted.Length);

        var cipher = new GcmBlockCipher(
            new AesEngine()
            );

        var parameters = new AeadParameters(
            key: new KeyParameter(_key),
            macSize: TAG_SIZE * 8,
            nonce: nonce
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
            return Array.Empty<byte>(); // for simplicity
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Array.Fill<byte>(_key, 0);
    }
}
