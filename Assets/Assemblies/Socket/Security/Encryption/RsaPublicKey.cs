#nullable enable
using System;
using System.Linq;
using System.Security.Cryptography;

namespace Larnix.Socket.Security.Encryption;

internal class RsaPublicKey : IEncryptor
{
    private readonly byte[] _modulus;
    private readonly byte[] _exponent;

    public RsaPublicKey(byte[] keyBytes)
    {
        int offset = 0;

        _modulus = keyBytes[offset..(offset += 256)];
        _exponent = keyBytes[offset..(offset += 8)];
    }

    public byte[] Encrypt(byte[] plaintext)
    {
        try
        {
            byte[] aesKey = new byte[32];
            RandomNumberGenerator.Fill(aesKey);

            AesKey aes = new(aesKey);

            using var rsa = RSA.Create();
            rsa.ImportParameters(ExportParameters());

            byte[] head = rsa.Encrypt(
                aes.Export(),
                RSAEncryptionPadding.OaepSHA256);

            byte[] body = aes.Encrypt(plaintext);

            return head
                .Concat(body)
                .ToArray();
        }
        catch (Exception)
        {
            return Array.Empty<byte>();
        }
    }

    public byte[] Export()
    {
        return _modulus
            .Concat(_exponent)
            .ToArray();
    }

    protected RSAParameters ExportParameters()
    {
        static byte[] CutLeadingZeros(byte[] bytes)
        {
            return bytes
                .SkipWhile(b => b == 0)
                .ToArray();
        }

        return new RSAParameters
        {
            Modulus = _modulus,
            Exponent = CutLeadingZeros(_exponent)
        };
    }
}
