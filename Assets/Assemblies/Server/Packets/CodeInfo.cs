#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(0x04)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct CodeInfo : ISanitizable<CodeInfo>
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
        Code = Enum.IsDefined(typeof(Info), code) ? code : Info.Empty;
    }

    public CodeInfo Sanitize()
    {
        return new CodeInfo(Code);
    }
}
