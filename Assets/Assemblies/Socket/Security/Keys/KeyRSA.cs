#nullable enable
using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Larnix.Core.Utils;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Server.Interfaces;

namespace Larnix.Socket.Security.Keys;

internal class KeyRsa : IKey
{
    private readonly RSA? _rsa;
    private readonly KeyMode _mode;

    private bool _disposed;

    private enum KeyMode
    {
        Full,
        PublicOnly,
        PublicBootstrap
    }

    private KeyRsa(RSA? rsa, KeyMode mode)
    {
        if (mode != KeyMode.PublicBootstrap && rsa == null)
            throw new ArgumentNullException(nameof(rsa), "RSA instance cannot be null for non-bootstrap modes.");

        _rsa = rsa;
        _mode = mode;
    }

    // TODO: Remove BouncyCastle dependency from this method
    public static KeyRsa FromSecretRepo(ISecretRepository repo, string key)
    {
        AsymmetricCipherKeyPair? keyPair;

        string? text = repo.ReadSecret(key);
        if (text == null || (keyPair = RsaHelpers.ParseRSA(text)) == null)
        {
            var keyGen = new RsaKeyPairGenerator();
            keyGen.Init(
                new KeyGenerationParameters(
                    new SecureRandom(), 2048
                    )
                );

            keyPair = keyGen.GenerateKeyPair();
            text = RsaHelpers.ConvertKeyPairToPem(keyPair);
            repo.StoreSecret(key, text);
        }

        return new KeyRsa(
            rsa: RsaHelpers.BouncyToRSA(keyPair),
            mode: KeyMode.Full
            );
    }

    public static KeyRsa FromPublicStruct(in FixedRsaPublic rsaPublicKey)
    {
        RSA rsa = RSA.Create();

        byte[] keyBytes = rsaPublicKey.Bytes264();

        try
        {
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = keyBytes[..256],
                Exponent = ArrayUtils.RemoveLeadingZeros(keyBytes[256..])
            });
        }
        catch
        {
            rsa.Dispose();

            return new KeyRsa(
                rsa: null,
                mode: KeyMode.PublicBootstrap
                );
        }

        return new KeyRsa(
            rsa: rsa,
            mode: KeyMode.PublicOnly
            );
    }

    public FixedRsaPublic ExportPublicKey()
    {
        if (_mode != KeyMode.Full)
            throw new InvalidOperationException($"Cannot export RSA key with mode {_mode}!");

        RSAParameters parameters = _rsa!.ExportParameters(false);

        byte[] bytes264 = ArrayUtils.MegaConcat(
            parameters.Modulus,
            ArrayUtils.AddLeadingZeros(parameters.Exponent, 8)
            );

        return FixedRsaPublic.FromBytes(bytes264);
    }

    public T CloneKey<T>() where T : IKey
    {
        RSA? copy = null;

        switch (_mode)
        {
            case KeyMode.Full:
                copy = RSA.Create();
                copy.ImportParameters(_rsa!.ExportParameters(true));
                break;

            case KeyMode.PublicOnly:
                copy = RSA.Create();
                copy.ImportParameters(_rsa!.ExportParameters(false));
                break;
        }

        return (T)(IKey)new KeyRsa(copy, _mode);
    }

    public byte[] Encrypt(byte[] plaintext)
    {
        return _mode switch
        {
            KeyMode.Full or KeyMode.PublicOnly
                => _rsa!.Encrypt(plaintext, RSAEncryptionPadding.OaepSHA1),
            _ => Array.Empty<byte>()
        };
    }

    public byte[] Decrypt(byte[] ciphertext)
    {
        if (_mode != KeyMode.Full)
            throw new InvalidOperationException("RSA decryption requires a private key!");

        try
        {
            return _rsa!.Decrypt(ciphertext, RSAEncryptionPadding.OaepSHA1);
        }
        catch (CryptographicException)
        {
            return Array.Empty<byte>();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _rsa?.Dispose();
    }
}
