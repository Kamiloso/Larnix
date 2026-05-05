#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Tools;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Packets;

[CmdId(-6)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct P_LoginTry : ISanitizable<P_LoginTry>
{
    public Credentials Credentials { get; }
    public FixedString64 NewPassword { get; }
    public boolsrl IsPasswordChange { get; }

    private readonly byte _padding = 0xFF; // prevent null-trimming optimizations at the end

    public P_LoginTry(Credentials credentials, in FixedString64 newPassword, boolsrl isPasswordChange)
    {
        Credentials = Sanitizer.Filter(credentials);
        NewPassword = SocketSanitizer.ToGoodPassword(newPassword);
        IsPasswordChange = Sanitizer.Filter(isPasswordChange);
    }

    public static P_LoginTry AsLogin(in Credentials credentials)
    {
        return new P_LoginTry(credentials, default, false);
    }

    public static P_LoginTry AsPasswordChange(in Credentials credentials, in FixedString64 newPassword)
    {
        return new P_LoginTry(credentials, newPassword, true);
    }

    public P_LoginTry Sanitize()
    {
        return new P_LoginTry(Credentials, NewPassword, IsPasswordChange);
    }
}
