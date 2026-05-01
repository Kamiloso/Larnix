#nullable enable
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Packets;

[CmdId(-8)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Start
{
    private readonly byte _filler;
}
