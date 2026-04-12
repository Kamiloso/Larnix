#nullable enable
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;
using Larnix.Socket.Payload.Structs;

namespace Larnix.Socket.Payload.Packets;

[CmdId(-5)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct A_ServerInfo : ISanitizable<A_ServerInfo>
{
    public ServerInfo Info { get; }
    public long ChallengeId { get; }

    public bool FreeUserSlot() => ChallengeId == 0;
    public bool UserExists() => ChallengeId != 0;

    public A_ServerInfo(in ServerInfo info, long challengeId)
    {
        Info = info.Sanitize();
        ChallengeId = challengeId;
    }

    public A_ServerInfo Sanitize()
    {
        return new A_ServerInfo(Info, ChallengeId);
    }
}
