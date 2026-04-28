#nullable enable
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Larnix.Socket.Security;

internal static class RsaHelpers
{
    public static AsymmetricCipherKeyPair? ParseRSA(string text)
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

    public static string ConvertKeyPairToPem(AsymmetricCipherKeyPair keyPair)
    {
        using var stringWriter = new StringWriter();

        var pemWriter = new PemWriter(stringWriter);
        pemWriter.WriteObject(keyPair);
        pemWriter.Writer.Flush();
        return stringWriter.ToString();
    }

    public static RSA BouncyToRSA(AsymmetricCipherKeyPair keyPair)
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
}
