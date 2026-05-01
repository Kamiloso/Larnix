#nullable enable
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Packets;

[CmdId(-2)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Stop
{
    private readonly byte _filler;
}
