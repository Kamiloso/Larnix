#nullable enable
using System;
using Larnix.Model.Utils;
using Larnix.Core.Utils;
using Larnix.Core;
using Version = Larnix.Model.Version;

namespace Larnix.Socket.Packets.Control;

internal sealed class A_ServerInfo : Payload
{
    private const int SIZE = 264 + 2 + 2 + 4 + 8 + 8 + 8 + 256 + 32;

    public byte[] PublicKey => new Span<byte>(Bytes, 0, 264).ToArray();
    public ushort CurrentPlayers => Binary<ushort>.Deserialize(Bytes, 264);
    public ushort MaxPlayers => Binary<ushort>.Deserialize(Bytes, 266);
    public Version GameVersion => Binary<Version>.Deserialize(Bytes, 268);
    public long ChallengeID => Binary<long>.Deserialize(Bytes, 272);
    public long Timestamp => Binary<long>.Deserialize(Bytes, 280);
    public long RunID => Binary<long>.Deserialize(Bytes, 288);
    public String256 Motd => Binary<String256>.Deserialize(Bytes, 296);
    public String32 HostUser => Binary<String32>.Deserialize(Bytes, 552);
    public bool MayRegister => Code != 0;

    public A_ServerInfo(byte[] publicKey, ushort currentPlayers, ushort maxPlayers, Version gameVersion, long challengeID, long timestamp, long runID,
        in String256 motd, in String32 hostUser, bool mayRegister)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            publicKey?.Length == 264 ? publicKey : throw new ArgumentException("PublicKey must have length of exactly 264 bytes."),
            Binary<ushort>.Serialize(currentPlayers),
            Binary<ushort>.Serialize(maxPlayers),
            Binary<Version>.Serialize(gameVersion),
            Binary<long>.Serialize(challengeID),
            Binary<long>.Serialize(timestamp),
            Binary<long>.Serialize(runID),
            Binary<String256>.Serialize(motd),
            Binary<String32>.Serialize(hostUser)
            ), code: (byte)(mayRegister ? 1 : 0));
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE &&
            Validation.IsGoodText<String256>(Motd) &&
            Validation.IsGoodText<String32>(HostUser);
    }
}
