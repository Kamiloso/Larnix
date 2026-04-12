#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Packets;

[CmdId(-8)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Start : ISanitizable<Start>
{
    private readonly byte _filler;

    public Start()
    {
        _filler = 0;
    }

    public Start Sanitize()
    {
        return new Start();
    }
}
