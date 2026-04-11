#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Packets.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Packets.Structs;

[CmdId(-3)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DebugMessage
{
    public FixedString1024 Message { get; }

    public DebugMessage(in FixedString1024 message)
    {
        Message = message;
    }
}
