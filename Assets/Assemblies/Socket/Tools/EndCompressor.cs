#nullable enable
using Larnix.Core.Serialization;
using System;

namespace Larnix.Socket.Tools;

/// <summary>
/// This is used to compress payload ending with nulls.
/// It may sometimes greatly reduce the size of a packet.
/// Be careful when putting security sensitive data at the end
/// of your payload! It may reveal trailing null count to the attacker.
/// </summary>
internal static class EndCompressor
{
    public static byte[] Compress(byte[] plaindata)
    {
        ushort nulls = 0;

        while (nulls < plaindata.Length)
        {
            byte b = plaindata[plaindata.Length - 1 - nulls];
            if (b != 0 || nulls == ushort.MaxValue)
            {
                break;
            }
            nulls++;
        }

        byte[] target = new byte[plaindata.Length - nulls + 2];

        Buffer.BlockCopy(plaindata, 0, target, 0, plaindata.Length - nulls);
        Buffer.BlockCopy(Binary<ushort>.Serialize(nulls), 0, target, target.Length - 2, 2);

        return target;
    }

    public static byte[] Decompress(byte[] compressed)
    {
        if (compressed.Length < 2)
        {
            return compressed[..];
        }

        byte[] target = new byte[SizeAfterDecompression(compressed)];
        Buffer.BlockCopy(compressed, 0, target, 0, compressed.Length - 2);
        return target;
    }

    public static int SizeAfterDecompression(byte[] compressed)
    {
        if (compressed.Length < 2)
        {
            return compressed.Length;
        }

        int offset = compressed.Length - 2;
        return offset + Binary<ushort>.Deserialize(compressed, offset);
    }

    public static byte[] PartialDecompress(byte[] compressed, int from, int length)
    {
        if (from < 0 || length < 0)
        {
            throw new ArgumentOutOfRangeException(from < 0 ? nameof(from) : nameof(length), "Value must be non-negative.");
        }

        if (compressed.Length < 2)
        {
            return compressed[from..(from + length)];
        }

        int to = from + length;
        int cmprLngt = compressed.Length - 2;

        ushort nulls = Binary<ushort>.Deserialize(compressed, cmprLngt);
        if (to > cmprLngt + nulls)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length is too big for the given compressed data.");
        }
        
        if (from >= cmprLngt)
        {
            return new byte[length];
        }

        if (to <= cmprLngt)
        {
            return compressed[from..to];
        }

        byte[] target = new byte[length];
        Buffer.BlockCopy(compressed, from, target, 0, cmprLngt - from);
        return target;
    }
}
