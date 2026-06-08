#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Security.Encryption;
using System.Runtime.InteropServices;
using Buf32 = Larnix.Core.Serialization.FixedBuffer32<byte>;

namespace Larnix.Socket.Payload.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct FixedAes : ISanitizable<FixedAes>
{
    private Buf32 BufferAes { get; }

    public AesKey GetKey() => new(BufferAes.ToArray());

    private FixedAes(in Buf32 bufferAes)
    {
        BufferAes = Sanitizer.FillAndFilter<Buf32, byte>(bufferAes, 0);
    }

    public static FixedAes FromKey(AesKey key)
    {
        Buf32 buffer = new();
        buffer.AddRange(key.Export());

        return new FixedAes(buffer);
    }

    public FixedAes Sanitize()
    {
        return new FixedAes(BufferAes);
    }
}
