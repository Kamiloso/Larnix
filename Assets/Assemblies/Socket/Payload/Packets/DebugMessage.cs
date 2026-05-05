#nullable enable
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Packets;

[CmdId(-3)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct DebugMessage : ISanitizable<DebugMessage>
{
    public FixedString1024 Message { get; }

    public DebugMessage(in FixedString1024 message)
    {
        Message = Sanitizer.Filter(message);
    }

    public DebugMessage Sanitize()
    {
        return new DebugMessage(Message);
    }
}
