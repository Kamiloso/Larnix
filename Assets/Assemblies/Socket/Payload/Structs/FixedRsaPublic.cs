#nullable enable
using Larnix.Core.Serialization;
using Larnix.Core.Utils;
using System;
using System.Runtime.InteropServices;
using Buf8 = Larnix.Core.Serialization.FixedBuffer8<byte>;
using Buf256 = Larnix.Core.Serialization.FixedBuffer256<byte>;

namespace Larnix.Socket.Payload.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct FixedRsaPublic : ISanitizable<FixedRsaPublic>
{
    private Buf256 BufferPublicRsa1 { get; }
    private Buf8 BufferPublicRsa2 { get; }

    public byte[] Bytes264() => ArrayUtils.MegaConcat(
        BufferPublicRsa1.ToArray(),
        BufferPublicRsa2.ToArray()
        );

    private FixedRsaPublic(in Buf256 bufferPublicRsa1, in Buf8 bufferPublicRsa2)
    {
        BufferPublicRsa1 = Sanitizer.FillAndFilter<Buf256, byte>(bufferPublicRsa1, 0);
        BufferPublicRsa2 = Sanitizer.FillAndFilter<Buf8, byte>(bufferPublicRsa2, 0);
    }

    public static FixedRsaPublic FromBytes(byte[] bytes264)
    {
        if (bytes264.Length != 264)
        {
            throw new ArgumentException($"Wrong key length received.");
        }

        Buf256 buffer1 = new();
        buffer1.AddRange(bytes264.AsSpan(0, 256));

        Buf8 buffer2 = new();
        buffer2.AddRange(bytes264.AsSpan(256, 8));

        return new FixedRsaPublic(buffer1, buffer2);
    }

    public FixedRsaPublic Sanitize()
    {
        return new FixedRsaPublic(BufferPublicRsa1, BufferPublicRsa2);
    }
}
