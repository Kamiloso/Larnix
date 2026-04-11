#nullable enable
using Larnix.Core.Serialization;
using Larnix.Model;
using Larnix.Model.Utils;
using Larnix.Socket.Packets.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Packets.Structs;

[CmdId(-4)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct P_ServerInfo : ISanitizable<P_ServerInfo>
{
    public FixedString32 Nickname { get; }

    public P_ServerInfo(in FixedString32 nickname)
    {
        Nickname = Validation.IsGoodNickname(nickname) ? nickname : new FixedString32(GameInfo.ReservedNickname);
    }

    public P_ServerInfo Sanitize()
    {
        return new P_ServerInfo(Nickname);
    }
}
