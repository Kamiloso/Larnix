#nullable enable
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;
using Larnix.Socket.Payload.Structs;

namespace Larnix.Socket.Payload.Packets;

[CmdId(-1)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct AllowConnection : ISanitizable<AllowConnection>
{
    public Credentials Credentials { get; }
    public FixedAes AesKey { get; }

    private readonly byte _padding = 0xFF; // prevents null-trimming optimizations at the end

    public AllowConnection(in Credentials credentials, in FixedAes aesKey)
    {
        Credentials = Sanitizer.Filter(credentials);
        AesKey = Sanitizer.Filter(aesKey);
    }

    public AllowConnection Sanitize()
    {
        return new AllowConnection(Credentials, AesKey);
    }
}
