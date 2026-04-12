#nullable enable
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Payload;

namespace Larnix.Socket.Payload.Packets;

[CmdId(-1)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct AllowConnection : ISanitizable<AllowConnection>
{
    private readonly Credentials _credentials;
    private readonly FixedBuffer32<byte> _aesBuffer;

    private readonly byte _padding = 0xFF; // prevents null-trimming optimizations at the end

    public Credentials Credentials => _credentials;
    public byte[] AesKey => _aesBuffer.ToArray();

    public AllowConnection(in Credentials credentials, byte[] aesKey)
    {
        _credentials = credentials.Sanitize();

        FixedBuffer32<byte> aesBuffer = new();
        for (int i = 0; i < aesBuffer.Capacity; i++)
        {
            bool isSafe = i < aesKey.Length;
            aesBuffer.Push(isSafe ? aesKey[i] : (byte)0);
        }
        _aesBuffer = aesBuffer;
    }

    public AllowConnection Sanitize()
    {
        return new AllowConnection(Credentials, AesKey);
    }
}
