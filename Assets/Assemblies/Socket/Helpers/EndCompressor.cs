#nullable enable
using Larnix.Core.Serialization;
using System;

namespace Larnix.Socket.Security.Keys;

/// <summary>
/// This is used to compress payload ending with nulls.
/// It may sometimes greatly reduce the size of a packet.
/// Be careful when putting security sensitive data at the end
/// of your payload! It may reveal trailing null count to the attacker.
/// </summary>
internal static class EndCompressor
{
    public static byte[] Compress(byte[] plaintext)
    {
        ushort nulls = 0;

        while (nulls < plaintext.Length)
        {
            byte b = plaintext[plaintext.Length - 1 - nulls];
            if (b != 0 || nulls == ushort.MaxValue)
            {
                break;
            }
            nulls++;
        }

        byte[] target = new byte[plaintext.Length - nulls + 2];

        Buffer.BlockCopy(plaintext, 0, target, 0, plaintext.Length - nulls);
        Buffer.BlockCopy(Binary<ushort>.Serialize(nulls), 0, target, target.Length - 2, 2);

        return target;
    }

    public static byte[] Decompress(byte[] ciphertext)
    {
        if (ciphertext.Length < 2)
        {
            return ciphertext[..];
        }

        byte[] target = new byte[SizeAfterDecompression(ciphertext)];
        Buffer.BlockCopy(ciphertext, 0, target, 0, ciphertext.Length - 2);
        return target;
    }

    public static int SizeAfterDecompression(byte[] ciphertext)
    {
        if (ciphertext.Length < 2)
        {
            return ciphertext.Length;
        }

        int offset = ciphertext.Length - 2;
        return offset + Binary<ushort>.Deserialize(ciphertext, offset);
    }
}
