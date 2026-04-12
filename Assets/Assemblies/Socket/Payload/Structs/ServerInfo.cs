#nullable enable
using Larnix.Core;
using Larnix.Core.Serialization;
using Larnix.Model;
using Larnix.Model.Utils;
using System.Runtime.InteropServices;
using Version = Larnix.Core.Version;

namespace Larnix.Socket.Payload.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct ServerInfo : ISanitizable<ServerInfo>
{
    private readonly byte _mayRegister;
    private readonly ushort _players;
    private readonly ushort _maxPlayers;
    private readonly Version _gameVersion;
    private readonly long _timestamp;
    private readonly long _runId;
    private readonly FixedString256 _motd;
    private readonly FixedString32 _hostUser;
    private readonly FixedRsaPublic _rsaPublicKey;

    public bool MayRegister => _mayRegister != 0;
    public ushort Players => _players;
    public ushort MaxPlayers => _maxPlayers;
    public Version GameVersion => _gameVersion;
    public long Timestamp => _timestamp;
    public long RunId => _runId;
    public FixedString256 Motd => _motd;
    public FixedString32 HostUser => _hostUser;
    public FixedRsaPublic RsaPublicKey => _rsaPublicKey;

    public ServerInfo(bool mayRegister, ushort players, ushort maxPlayers, Version gameVersion, long timestamp, long runId, in FixedString256 motd, in FixedString32 hostUser, in FixedRsaPublic rsaPublicKey)
    {
        _mayRegister = (byte)(mayRegister ? 1 : 0);
        _players = players;
        _maxPlayers = maxPlayers;
        _gameVersion = gameVersion;
        _timestamp = timestamp;
        _runId = runId;
        _motd = Validation.IsGoodText<FixedString256>(motd) ? motd : new FixedString256(GameInfo.DefaultMotd);
        _hostUser = Validation.IsGoodNickname(hostUser) ? hostUser : new FixedString32(GameInfo.ReservedNickname);
        _rsaPublicKey = rsaPublicKey.Sanitize();
    }

    public ServerInfo Sanitize()
    {
        return new ServerInfo(MayRegister, Players, MaxPlayers, GameVersion, Timestamp, RunId, Motd, HostUser, RsaPublicKey);
    }
}
