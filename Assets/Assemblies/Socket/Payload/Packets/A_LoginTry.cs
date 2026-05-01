#nullable enable
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Packets;

[CmdId(-7)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct A_LoginTry : ISanitizable<A_LoginTry>
{
    public boolsrl Success { get; }

    public A_LoginTry(boolsrl success)
    {
        Success = Sanitizer.Filter(success);
    }

    public A_LoginTry Sanitize()
    {
        return new A_LoginTry(Success);
    }
}
