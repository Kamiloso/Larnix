#nullable enable
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct FixedAes : ISanitizable<FixedAes>
{
    private readonly FixedBuffer32<byte> _bufferAes;

    public byte[] Bytes32 => _bufferAes.ToArray();

    public FixedAes(byte[] bytes32)
    {
        FixedBuffer32<byte> buffer = new();
        for (int i = 0; i < buffer.Capacity; i++)
        {
            byte b = i < bytes32.Length ? bytes32[i] : (byte)0;
            buffer.Push(b);
        }
        _bufferAes = buffer;
    }

    public FixedAes Sanitize()
    {
        return new FixedAes(Bytes32);
    }
}
