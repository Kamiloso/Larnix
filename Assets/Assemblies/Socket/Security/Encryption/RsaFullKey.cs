#nullable enable
using Larnix.Socket.Security.Encryption;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace Larnix.Socket.Security.Encryption;

internal class RsaFullKey : RsaPublicKey, IFullEncryptor
{
    private readonly byte[] _privateExponent;
    private readonly byte[] _prime1;
    private readonly byte[] _prime2;
    private readonly byte[] _dp;
    private readonly byte[] _dq;
    private readonly byte[] _inverseQ;

    public RsaFullKey(byte[] keyBytes) : base(keyBytes)
    {
        int offset = 264;

        _privateExponent = keyBytes[offset..(offset += 256)];
        _prime1 = keyBytes[offset..(offset += 128)];
        _prime2 = keyBytes[offset..(offset += 128)];
        _dp = keyBytes[offset..(offset += 128)];
        _dq = keyBytes[offset..(offset += 128)];
        _inverseQ = keyBytes[offset..(offset += 128)];
    }

    public static RsaFullKey Generate()
    {
        using var rsa = RSA.Create(2048);
        RSAParameters parameters = rsa.ExportParameters(true);

        static byte[] Pad(byte[] bytes, int target)
        {
            return new byte[target]
                .Concat(bytes)
                .Skip(bytes.Length)
                .ToArray();
        }

        byte[] keyBytes = Pad(parameters.Modulus, 256)
            .Concat(Pad(parameters.Exponent, 8))
            .Concat(Pad(parameters.D, 256))
            .Concat(Pad(parameters.P, 128))
            .Concat(Pad(parameters.Q, 128))
            .Concat(Pad(parameters.DP, 128))
            .Concat(Pad(parameters.DQ, 128))
            .Concat(Pad(parameters.InverseQ, 128))
            .ToArray();

        return new RsaFullKey(keyBytes);
    }

    public byte[] Decrypt(byte[] ciphertext)
    {
        try
        {
            if (ciphertext.Length < 256)
            {
                throw new ArgumentException($"Ciphertext is too short.", nameof(ciphertext));
            }

            byte[] head = ciphertext[..256];
            byte[] body = ciphertext[256..];

            using var rsa = RSA.Create();
            rsa.ImportParameters(ExportParameters());

            byte[] aesKey = rsa.Decrypt(
                head,
                RSAEncryptionPadding.OaepSHA256);

            AesKey aes = new(aesKey);
            return aes.Decrypt(body);
        }
        catch (Exception)
        {
            return Array.Empty<byte>();
        }
    }

    public new byte[] Export()
    {
        return base.Export()
            .Concat(_privateExponent)
            .Concat(_prime1)
            .Concat(_prime2)
            .Concat(_dp)
            .Concat(_dq)
            .Concat(_inverseQ)
            .ToArray();
    }

    protected new RSAParameters ExportParameters()
    {
        var parameters = base.ExportParameters();
        return new RSAParameters
        {
            Modulus = parameters.Modulus,
            Exponent = parameters.Exponent,
            D = _privateExponent,
            P = _prime1,
            Q = _prime2,
            DP = _dp,
            DQ = _dq,
            InverseQ = _inverseQ
        };
    }
}
