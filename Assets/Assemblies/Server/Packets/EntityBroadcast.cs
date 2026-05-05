#nullable enable
using Larnix.Core.Serialization;
using Larnix.Model.Entities.Structs;
using Larnix.Server.Packets.Structs;
using Larnix.Socket.Payload;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(5)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct EntityBroadcast : ISanitizable<EntityBroadcast>
{
    public uint PacketFixedIndex { get; }
    public FixedBuffer1024<BroadcastRecord> BroadcastRecords { get; }

    private EntityBroadcast(uint packetFixedIndex, in FixedBuffer1024<BroadcastRecord> broadcastRecords)
    {
        PacketFixedIndex = packetFixedIndex;
        BroadcastRecords = Sanitizer.Filter(broadcastRecords);
    }

    public static IEnumerable<EntityBroadcast> CreateList(uint packetFixedIndex, BroadcastRecord[] broadcastRecords)
    {
        int s1 = 0; // start
        int l1 = 0; // length
        do
        {
            s1 += l1;
            l1 = 0;

            FixedBuffer1024<BroadcastRecord> buffer = new();
            while (s1 + l1 < broadcastRecords.Length)
            {
                buffer.Add(broadcastRecords[s1 + l1++]);
            }

            yield return new EntityBroadcast(packetFixedIndex, buffer);

        } while (l1 > 0);
    }

    public Dictionary<ulong, EntityHeader> ToHeaderDictionary()
    {
        var buffer = BroadcastRecords;

        Span<BroadcastRecord> span = stackalloc BroadcastRecord[buffer.Count];
        buffer.ReadInto(span);

        Dictionary<ulong, EntityHeader> dict = new();
        foreach (var record in span)
        {
            dict.Add(record.Uid, record.Header);
        }
        return dict;
    }

    public Dictionary<ulong, uint> ToFixedFrameDictionary()
    {
        var buffer = BroadcastRecords;

        Span<BroadcastRecord> span = stackalloc BroadcastRecord[buffer.Count];
        buffer.ReadInto(span);

        Dictionary<ulong, uint> dict = new();
        foreach (var record in span)
        {
            dict.Add(record.Uid, record.FixedFrame);
        }
        return dict;
    }

    public EntityBroadcast Sanitize()
    {
        return new EntityBroadcast(PacketFixedIndex, BroadcastRecords);
    }
}
