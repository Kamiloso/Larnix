#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Packets.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Packets.Structs;

[CmdId(-7)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct A_LoginTry : ISanitizable<A_LoginTry>
{
    private readonly byte _success;

    public bool Success => _success != 0;

    public A_LoginTry(bool success)
    {
        _success = (byte)(success ? 1 : 0);
    }

    public A_LoginTry Sanitize()
    {
        return new A_LoginTry(Success);
    }
}
