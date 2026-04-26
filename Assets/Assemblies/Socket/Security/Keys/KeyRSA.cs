#nullable enable
using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System.IO;
using Larnix.Core.Utils;
using Larnix.Socket.Backend.Utility;

namespace Larnix.Socket.Security.Keys;

// TODO: rework bootstrap key system!!!

public class KeyRSA : IEncryptionKey, IDisposable
{
    private readonly RSA _rsa;
    private readonly bool _isFullKey;
    private readonly bool _isBootstrap; // bootstrap key always returns empty bytes

    private bool _disposed;

    private KeyRSA(RSA rsa, bool isFullKey, bool isBootstrap = false)
    {
        _rsa = rsa;
        _isFullKey = isFullKey;
        _isBootstrap = isBootstrap;
    }

    public static KeyRSA FromSecretRepo(ISecretRepository repo, string key)
    {
        AsymmetricCipherKeyPair? keyPair;

        string? text = repo.ReadSecret(key);
        if (text == null || (keyPair = ParseRSA(text)) == null)
        {
            // generate key
            var keyGen = new RsaKeyPairGenerator();
            keyGen.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
            keyPair = keyGen.GenerateKeyPair();

            text = ConvertKeyPairToPem(keyPair);
            repo.StoreSecret(key, text);
        }

        return new KeyRSA(
            rsa: BouncyToRSA(keyPair),
            isFullKey: true
            );
    }

    public static KeyRSA FromPublicBytes(byte[] keyBytes)
    {
        RSA rsa = RSA.Create();

        try
        {
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = keyBytes[..256],
                Exponent = ArrayUtils.RemoveLeadingZeros(keyBytes[256..])
            });

            return new KeyRSA(
                rsa: rsa,
                isFullKey: false
                );
        }
        catch
        {
            return new KeyRSA(
                rsa: rsa,
                isFullKey: false,
                isBootstrap: true
                );
        }
    }

    public byte[] ExportPublicKey()
    {
        if (_isBootstrap)
            throw new InvalidOperationException("Cannot export bootstrap key!");

        var parameters = _rsa.ExportParameters(false);

        return ArrayUtils.MegaConcat(
            parameters.Modulus,
            ArrayUtils.AddLeadingZeros(parameters.Exponent, 8)
            );
    }

    public byte[] Encrypt(byte[] plaintext)
    {
        return _isBootstrap
            ? Array.Empty<byte>()
            : _rsa.Encrypt(plaintext, RSAEncryptionPadding.OaepSHA1);
    }

    public byte[] Decrypt(byte[] ciphertext)
    {
        if (!_isFullKey)
            throw new InvalidOperationException("Cannot decrypt using public key!");

        if (_isBootstrap)
            return Array.Empty<byte>();

        try
        {
            return _rsa.Decrypt(ciphertext, RSAEncryptionPadding.OaepSHA1);
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

            _rsa?.Dispose();
        }
    }

#region Static Helpers

    private static AsymmetricCipherKeyPair? ParseRSA(string text)
    {
        try
        {
            using var reader = new StringReader(text);

            var pemReader = new PemReader(reader);
            var obj = pemReader.ReadObject();

            return obj is AsymmetricCipherKeyPair keyPair
                ? keyPair
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ConvertKeyPairToPem(AsymmetricCipherKeyPair keyPair)
    {
        using var stringWriter = new StringWriter();

        var pemWriter = new PemWriter(stringWriter);
        pemWriter.WriteObject(keyPair);
        pemWriter.Writer.Flush();
        return stringWriter.ToString();
    }

    private static RSA BouncyToRSA(AsymmetricCipherKeyPair keyPair)
    {
        var priv = (RsaPrivateCrtKeyParameters)keyPair.Private;

        int modulusSize = (priv.Modulus.BitLength + 7) / 8;
        int halfSize = (modulusSize + 1) / 2;

        var rsaParams = new RSAParameters
        {
            Modulus = Pad(priv.Modulus.ToByteArrayUnsigned(), modulusSize),
            Exponent = priv.PublicExponent.ToByteArrayUnsigned(),
            D = Pad(priv.Exponent.ToByteArrayUnsigned(), modulusSize),
            P = Pad(priv.P.ToByteArrayUnsigned(), halfSize),
            Q = Pad(priv.Q.ToByteArrayUnsigned(), halfSize),
            DP = Pad(priv.DP.ToByteArrayUnsigned(), halfSize),
            DQ = Pad(priv.DQ.ToByteArrayUnsigned(), halfSize),
            InverseQ = Pad(priv.QInv.ToByteArrayUnsigned(), halfSize)
        };

        var rsa = RSA.Create();
        rsa.ImportParameters(rsaParams);
        return rsa;
    }

    private static byte[] Pad(byte[] input, int size)
    {
        if (input.Length == size)
            return input;

        if (input.Length > size)
            throw new CryptographicException("RSA parameter larger than expected.");

        var padded = new byte[size];
        Buffer.BlockCopy(input, 0, padded, size - input.Length, input.Length);
        return padded;
    }

#endregion

}
