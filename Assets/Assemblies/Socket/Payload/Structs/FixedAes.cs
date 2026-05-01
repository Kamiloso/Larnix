#nullable enable
using Larnix.Core.Serialization;
using System;
using System.Runtime.InteropServices;
using Buf32 = Larnix.Core.Serialization.FixedBuffer32<byte>;

namespace Larnix.Socket.Payload.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct FixedAes : ISanitizable<FixedAes>
{
    private Buf32 BufferAes { get; }

    public byte[] Bytes32() => BufferAes.ToArray(); // secure data - clean up after use recommended

    private FixedAes(in Buf32 bufferAes)
    {
        BufferAes = Sanitizer.FilterToFull<Buf32, byte>(bufferAes);
    }

    public static FixedAes FromBytes(byte[] bytes32)
    {
        if (bytes32.Length != 32)
        {
            throw new ArgumentException($"Wrong key length received.");
        }

        Buf32 buffer = new();
        buffer.AddRange(bytes32);

        return new FixedAes(buffer);
    }

    public FixedAes Sanitize()
    {
        return new FixedAes(BufferAes);
    }
}
