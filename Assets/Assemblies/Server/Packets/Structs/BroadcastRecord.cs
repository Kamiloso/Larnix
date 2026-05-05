#nullable enable
using Larnix.Core.Serialization;
using Larnix.Model.Entities.Structs;
using System.Runtime.InteropServices;
using Larnix.Model.Entities;
using System;

namespace Larnix.Server.Packets.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct BroadcastRecord : ISanitizable<BroadcastRecord>
{
    private readonly ulong _uid;
    private readonly EntityHeaderCompressed _headerCmpr;
    private readonly uint _fixedFrame;

    public ulong Uid => _uid;
    public EntityHeader Header => _headerCmpr.Header;
    public uint FixedFrame => _fixedFrame;

    public bool IsPlayer() => Header.Id == EntityID.Player;

    private BroadcastRecord(ulong uid, in EntityHeader header, uint fixedFrame)
    {
        _uid = uid;
        _headerCmpr = new EntityHeaderCompressed(header);
        _fixedFrame = fixedFrame;
    }

    public static BroadcastRecord CreatePlayer(ulong uid, in EntityHeader header, uint fixedFrame)
    {
        if (header.Id != EntityID.Player)
            throw new ArgumentException($"Provided header must be of type Player.", nameof(header));

        return new BroadcastRecord(uid, header, fixedFrame);
    }

    public static BroadcastRecord CreateEntity(ulong uid, in EntityHeader header)
    {
        if (header.Id == EntityID.Player)
            throw new ArgumentException($"Provided header must not be of type Player.", nameof(header));

        return new BroadcastRecord(uid, header, 0);
    }

    public BroadcastRecord Sanitize()
    {
        return new BroadcastRecord(Uid, Header, FixedFrame);
    }
}
