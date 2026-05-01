#nullable enable
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Packets;

[CmdId(0)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct None
{
    private readonly byte _filler;
}
