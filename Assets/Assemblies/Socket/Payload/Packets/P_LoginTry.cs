#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Payload.Structs;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Packets;

[CmdId(-6)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct P_LoginTry : ISanitizable<P_LoginTry>
{
    public Credentials Credentials { get; }
    public FixedString64 NewPassword { get; }

    private readonly byte _padding = 0xFF; // prevent null-trimming optimizations at the end

    public bool IsPasswordChange() => NewPassword != Credentials.Password;

    public P_LoginTry(Credentials credentials, in FixedString64 newPassword)
    {
        Credentials = Sanitizer.Filter(credentials);
        NewPassword = SocketSanitizer.ToGoodPassword(newPassword);
    }

    public static P_LoginTry AsLogin(in Credentials credentials)
    {
        return new P_LoginTry(credentials, credentials.Password);
    }

    /// <summary>
    /// If the new password is the same as the current one, payload will be implicitly downgraded to the login attempt.
    /// </summary>
    public static P_LoginTry AsPasswordChange(in Credentials credentials, in FixedString64 newPassword)
    {
        return new P_LoginTry(credentials, newPassword);
    }

    public P_LoginTry Sanitize()
    {
        return new P_LoginTry(Credentials, NewPassword);
    }
}
