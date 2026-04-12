#nullable enable
using Larnix.Core.Serialization;
using Larnix.Core.Utils;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct FixedRsaPublic : ISanitizable<FixedRsaPublic>
{
    private readonly FixedBuffer256<byte> _bufferPublicRsa1;
    private readonly FixedBuffer8<byte> _bufferPublicRsa2;

    public byte[] Bytes264 => ArrayUtils.MegaConcat(_bufferPublicRsa1.ToArray(), _bufferPublicRsa2.ToArray());

    public FixedRsaPublic(byte[] bytes264)
    {
        FixedBuffer256<byte> buffer1 = new();
        for (int i = 0; i < buffer1.Capacity; i++)
        {
            byte b = i < bytes264.Length ? bytes264[i] : (byte)0;
            buffer1.Push(b);
        }
        _bufferPublicRsa1 = buffer1;

        FixedBuffer8<byte> buffer2 = new();
        for (int i = 0; i < buffer2.Capacity; i++)
        {
            int j = i + buffer1.Capacity;
            byte b = j < bytes264.Length ? bytes264[j] : (byte)0;
            buffer2.Push(b);
        }
        _bufferPublicRsa2 = buffer2;
    }

    public FixedRsaPublic Sanitize()
    {
        return new FixedRsaPublic(Bytes264);
    }
}
