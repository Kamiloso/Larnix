#nullable enable
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;
using Version = Larnix.Core.Version;

namespace Larnix.Socket.Payload.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct ServerInfo : ISanitizable<ServerInfo>
{
    public boolsrl MayRegister { get; } // is registration enabled?
    public ushort Players { get; }
    public ushort MaxPlayers { get; }
    public Version GameVersion { get; }
    public long Timestamp { get; }
    public long RunId { get; }
    public FixedString256 Motd { get; }
    public FixedString32 HostUser { get; }
    public FixedRsaPublic RsaPublicKey { get; }

    public ServerInfo(boolsrl mayRegister, ushort players, ushort maxPlayers, Version gameVersion, long timestamp, long runId, in FixedString256 motd, in FixedString32 hostUser, in FixedRsaPublic rsaPublicKey)
    {
        MayRegister = Sanitizer.Filter(mayRegister);
        Players = players;
        MaxPlayers = maxPlayers;
        GameVersion = Sanitizer.Filter(gameVersion);
        Timestamp = timestamp;
        RunId = runId;
        Motd = SocketSanitizer.ToGoodMotd(motd);
        HostUser = SocketSanitizer.ToGoodNickname(hostUser);
        RsaPublicKey = Sanitizer.Filter(rsaPublicKey);
    }

    public ServerInfo Sanitize()
    {
        return new ServerInfo(MayRegister, Players, MaxPlayers, GameVersion, Timestamp, RunId, Motd, HostUser, RsaPublicKey);
    }
}
