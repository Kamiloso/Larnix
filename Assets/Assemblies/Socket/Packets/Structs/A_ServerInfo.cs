#nullable enable
using Larnix.Core.Serialization;
using Larnix.Model.Utils;
using Larnix.Socket.Packets.Payload;
using Version = Larnix.Model.Version;
using System.Runtime.InteropServices;
using Larnix.Core.Utils;
using Larnix.Model;

namespace Larnix.Socket.Packets.Structs;

[CmdId(-5)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct A_ServerInfo : ISanitizable<A_ServerInfo>
{
    private readonly byte _mayRegister;
    private readonly ushort _currentPlayers;
    private readonly ushort _maxPlayers;
    private readonly Version _gameVersion;
    private readonly long _challengeId;
    private readonly long _timestamp;
    private readonly long _runId;
    private readonly FixedString256 _motd;
    private readonly FixedString32 _hostUser;
    private readonly FixedBuffer256<byte> _rsaBuffer1;
    private readonly FixedBuffer8<byte> _rsaBuffer2;

    public bool MayRegister => _mayRegister != 0;
    public ushort CurrentPlayers => _currentPlayers;
    public ushort MaxPlayers => _maxPlayers;
    public Version GameVersion => _gameVersion;
    public long ChallengeId => _challengeId;
    public long Timestamp => _timestamp;
    public long RunId => _runId;
    public FixedString256 Motd => _motd;
    public FixedString32 HostUser => _hostUser;
    public byte[] RsaKey => ArrayUtils.MegaConcat(_rsaBuffer1.ToArray(), _rsaBuffer2.ToArray()); // 264 bytes = 256 + 8

    public A_ServerInfo(bool mayRegister, ushort currentPlayers, ushort maxPlayers, Version gameVersion, long challengeId, long timestamp, long runId, in FixedString256 motd, in FixedString32 hostUser, byte[] rsaKey)
    {
        _mayRegister = (byte)(mayRegister ? 1 : 0);
        _currentPlayers = currentPlayers;
        _maxPlayers = maxPlayers;
        _gameVersion = gameVersion;
        _challengeId = challengeId;
        _timestamp = timestamp;
        _runId = runId;
        _motd = Validation.IsGoodText<FixedString256>(motd) ? motd : new FixedString256(GameInfo.DefaultMotd);
        _hostUser = Validation.IsGoodNickname(hostUser) ? hostUser : new FixedString32(GameInfo.ReservedNickname);

        FixedBuffer256<byte> rsaBuffer1 = new();
        for (int i = 0; i < rsaBuffer1.Capacity; i++)
        {
            bool isSafe = i < rsaKey.Length;
            rsaBuffer1.Push(isSafe ? rsaKey[i] : (byte)0);
        }
        _rsaBuffer1 = rsaBuffer1;

        FixedBuffer8<byte> rsaBuffer2 = new();
        for (int i = 0; i < rsaBuffer2.Capacity; i++)
        {
            int j = i + rsaBuffer1.Capacity;
            bool isSafe = j < rsaKey.Length;
            rsaBuffer2.Push(isSafe ? rsaKey[j] : (byte)0);
        }
        _rsaBuffer2 = rsaBuffer2;
    }

    public A_ServerInfo Sanitize()
    {
        return new A_ServerInfo(MayRegister, CurrentPlayers, MaxPlayers, GameVersion, ChallengeId, Timestamp, RunId, Motd, HostUser, RsaKey);
    }
}
