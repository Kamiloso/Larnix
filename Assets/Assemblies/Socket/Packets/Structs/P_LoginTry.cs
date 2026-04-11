#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Packets.Payload;
using Larnix.Socket.Packets.Payload.Structs.Structs;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Packets.Structs;

[CmdId(-6)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct P_LoginTry : ISanitizable<P_LoginTry>
{
    private readonly Credentials _credentials;
    private readonly byte _hasNewPassword;
    private readonly FixedString64 _newPassword;

    private readonly byte _padding = 0xFF; // prevent null-trimming optimizations at the end

    public Credentials Credentials => _credentials;
    public FixedString64? NewPassword => _hasNewPassword != 0 ? _newPassword : null;

    public bool IsPasswordChangeRequest() => NewPassword.HasValue;

    public P_LoginTry(Credentials credentials, in FixedString64? newPassword = null)
    {
        _credentials = credentials.Sanitize();
        _hasNewPassword = (byte)(newPassword.HasValue ? 1 : 0);
        _newPassword = newPassword ?? new FixedString64();
    }

    public P_LoginTry Sanitize()
    {
        return new P_LoginTry(Credentials, NewPassword);
    }
}
