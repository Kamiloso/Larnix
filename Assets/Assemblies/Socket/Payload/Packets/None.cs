#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Packets;

[CmdId(0)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct None : ISanitizable<None>
{
    private readonly byte _filler;

    public None()
    {
        _filler = 0;
    }

    public None Sanitize()
    {
        return new None();
    }
}
