#nullable enable
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Security.KeyStructs;

namespace Larnix.Socket.Payload.Packets;

[CmdId(-1)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct AllowConnection : ISanitizable<AllowConnection>
{
    private readonly Credentials _credentials;
    private readonly FixedAes _aesKey;

    private readonly byte _padding = 0xFF; // prevents null-trimming optimizations at the end

    public Credentials Credentials => _credentials;
    public FixedAes AesKey => _aesKey;

    public AllowConnection(in Credentials credentials, in FixedAes aesKey)
    {
        _credentials = credentials.Sanitize();
        _aesKey = aesKey.Sanitize();
    }

    public AllowConnection Sanitize()
    {
        return new AllowConnection(Credentials, AesKey);
    }
}
