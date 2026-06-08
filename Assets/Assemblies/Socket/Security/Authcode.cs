#nullable enable
using Larnix.Core.Serialization;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Security.Cryptography;

namespace Larnix.Socket.Security;

// It should not be used for storage or anything like that.
// Convert to string and then do "new Authcode(...)" dynamically.
// It doesn't have to be ref struct, but it is since this
// class is NOT intended to be stored on heap.

internal readonly ref struct Authcode
{
    // HHHHHH-HHHHHH-SSSSSS-SSSSSO
    // H = verify segment   | 12 char | 9 bytes |
    // S = secret segment   | 11 char | 8 bytes |
    // O = checksum         |  1 char |

    private readonly byte[] _verify; // hashed public key for example
    private readonly byte[] _secret; // secret that only server and its users know

    public Authcode(byte[] verifable, long secret)
    {
        _verify = MemoryHardHash(verifable)[..9];
        _secret = Binary<long>.Serialize(secret);
    }

    public Authcode(string code)
    {
        if (!Regex.IsMatch(code,
            "^[0-9A-Za-z#&]{6}-[0-9A-Za-z#&]{6}-[0-9A-Za-z#&]{6}-[0-9A-Za-z#&]{6}$"))
            throw new ArgumentException("Authcode is invalid!");

        string[] parts = { code[0..6], code[7..13], code[14..20], code[21..27] };

        string verify = parts[0] + parts[1];
        string secret = parts[2] + parts[3][..5];
        char checksum = parts[3][5];

        if (Checksum(verify + secret) != checksum)
            throw new ArgumentException("Authcode has a wrong checksum!");

        _verify = ModifiedBase64.Decode(verify, 9);
        _secret = ModifiedBase64.Decode(secret, 8);
    }

    public bool Verify(byte[] verifable)
    {
        Authcode bootstrap = new(verifable, 0);
        return _verify.SequenceEqual(bootstrap._verify);
    }

    public long ExtractSecret()
    {
        return Binary<long>.Deserialize(_secret);
    }

    public override string ToString()
    {
        string verify = ModifiedBase64.Encode(_verify, 12);
        string secret = ModifiedBase64.Encode(_secret, 11);
        char checksum = Checksum(verify + secret);

        string raw = verify + secret + checksum;
        return $"{raw[0..6]}-{raw[6..12]}-{raw[12..18]}-{raw[18..24]}";
    }

    private static char Checksum(string str)
    {
        int sum = 0;
        foreach (char c in str)
        {
            int num = ModifiedBase64.ToIndex(c);
            sum = (sum + num) % 64;
        }
        return ModifiedBase64.ToChar(sum);
    }

    private static byte[] MemoryHardHash(byte[] data)
    {
        // TODO: use memory hashing alghorithm like Argon2 instead of SHA256
        // IMPORTANT!!!!

        using var sha256 = SHA256.Create();

        byte[] hash = data;
        for (int i = 0; i < 100_000; i++) // temporary, will be replaced with something better
        {
            hash = sha256.ComputeHash(hash);
        }

        return hash;
    }
}
