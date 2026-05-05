#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(4)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct CodeInfo : ISanitizable<CodeInfo>
{
    public Info Code { get; }

    public enum Info : byte
    {
        Empty = 0,
        YouDie = 1,
        RespawnMe = 2,
    }

    public CodeInfo(Info code)
    {
        Code = Sanitizer.Filter(code);
    }

    public CodeInfo Sanitize()
    {
        return new CodeInfo(Code);
    }
}
