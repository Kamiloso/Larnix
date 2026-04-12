#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Packets;

[CmdId(-2)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Stop : ISanitizable<Stop>
{
    private readonly byte _filler;

    public Stop()
    {
        _filler = 0;
    }

    public Stop Sanitize()
    {
        return new Stop();
    }
}
