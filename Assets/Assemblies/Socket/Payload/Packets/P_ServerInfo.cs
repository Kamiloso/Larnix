#nullable enable
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Packets;

[CmdId(-4)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct P_ServerInfo : ISanitizable<P_ServerInfo>
{
    public FixedString32 Nickname { get; }

    public P_ServerInfo(in FixedString32 nickname)
    {
        Nickname = SocketSanitizer.ToGoodNickname(nickname);
    }

    public P_ServerInfo Sanitize()
    {
        return new P_ServerInfo(Nickname);
    }
}
