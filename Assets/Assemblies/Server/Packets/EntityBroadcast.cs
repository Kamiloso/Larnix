using System;
using System.Collections.Generic;
using System.Linq;
using Larnix.Core.Binary;
using Larnix.Socket.Packets;
using Larnix.Model.Entities.Structs;
using Larnix.Server.Packets.Structs;
using Larnix.Core.Utils;

namespace Larnix.Server.Packets;

public sealed class EntityBroadcast : Payload
{
    private const int HEADER_SIZE = sizeof(uint) + sizeof(ushort) + sizeof(ushort);
    private const int ENTRY_A_SIZE = sizeof(ulong) + EntityHeaderCompressed.SIZE; // entity transforms entry
    private const int ENTRY_B_SIZE = sizeof(ulong) + sizeof(uint); // player fixed indexes entry
    private const int MAX_PAYLOAD_SIZE = 1400 - HEADER_SIZE; // max payload bytes excluding header

    public uint PacketFixedIndex => Primitives.FromBytes<uint>(Bytes, 0); // 4B
    public ushort EntityLength => Primitives.FromBytes<ushort>(Bytes, 4); // 2B
    public ushort PlayerFixedLength => Primitives.FromBytes<ushort>(Bytes, 6); // 2B
    public Dictionary<ulong, EntityHeader> EntityTransforms => GetDictionaryA(Bytes, EntityLength, HEADER_SIZE); // n * ENTRY_A_SIZE
    public Dictionary<ulong, uint> PlayerFixedIndexes => GetDictionaryB(Bytes, PlayerFixedLength, HEADER_SIZE + EntityLength * ENTRY_A_SIZE); // n * ENTRY_B_SIZE


    private EntityBroadcast(uint packetFixedIndex, Dictionary<ulong, EntityHeader> entityTransforms, Dictionary<ulong, uint> playerFixedIndexes, byte code = 0)
    {
        if (entityTransforms == null) entityTransforms = new();
        if (playerFixedIndexes == null) playerFixedIndexes = new();

        InitializePayload(ArrayUtils.MegaConcat(
            Primitives.GetBytes(packetFixedIndex),
            Primitives.GetBytes((ushort)entityTransforms.Count),
            Primitives.GetBytes((ushort)playerFixedIndexes.Count),
            SerializeDictionaryA(entityTransforms),
            SerializeDictionaryB(playerFixedIndexes)
            ), code);
    }

    public static List<EntityBroadcast> CreateList(uint packetFixedIndex, Dictionary<ulong, EntityHeader> entityTransforms, Dictionary<ulong, uint> playerFixedIndexes, byte code = 0)
    {
        if (entityTransforms == null) entityTransforms = new();
        if (playerFixedIndexes == null) playerFixedIndexes = new();

        List<EntityBroadcast> result = new();
        List<ulong> sendUIDs = entityTransforms.Keys.ToList();

        int idx = 0;
        while (idx < sendUIDs.Count)
        {
            Dictionary<ulong, EntityHeader> fragmentEntities = new();
            Dictionary<ulong, uint> fragmentFixed = new();

            int payloadBytes = 0; // current payload size in bytes

            // pack as many UIDs as possible until we hit MAX_PAYLOAD_SIZE
            while (idx < sendUIDs.Count)
            {
                ulong uid = sendUIDs[idx];
                int added = ENTRY_A_SIZE;
                bool hasFixed = playerFixedIndexes.ContainsKey(uid);
                if (hasFixed) added += ENTRY_B_SIZE;

                if (payloadBytes + added > MAX_PAYLOAD_SIZE)
                    break; // can't add more without exceeding max payload size

                fragmentEntities[uid] = entityTransforms[uid];
                if (hasFixed) fragmentFixed[uid] = playerFixedIndexes[uid];

                payloadBytes += added;
                idx++;
            }

            result.Add(new EntityBroadcast(packetFixedIndex, fragmentEntities, fragmentFixed, code));
        }

        return result;
    }

    private static Dictionary<ulong, EntityHeader> GetDictionaryA(byte[] bytes, int count, int offset = 0)
    {
        var result = new Dictionary<ulong, EntityHeader>();
        for (int i = 0; i < count; i++)
        {
            ulong key = Primitives.FromBytes<ulong>(bytes, i * ENTRY_A_SIZE + 0 + offset);
            EntityHeader value = Structures.FromBytes<EntityHeaderCompressed>(bytes, i * ENTRY_A_SIZE + sizeof(ulong) + offset).Header;
            result[key] = value;
        }
        return result;
    }

    private static Dictionary<ulong, uint> GetDictionaryB(byte[] bytes, int count, int offset = 0)
    {
        var result = new Dictionary<ulong, uint>();
        for (int i = 0; i < count; i++)
        {
            ulong key = Primitives.FromBytes<ulong>(bytes, i * ENTRY_B_SIZE + 0 + offset);
            uint value = Primitives.FromBytes<uint>(bytes, i * ENTRY_B_SIZE + sizeof(ulong) + offset);
            result[key] = value;
        }
        return result;
    }

    private static byte[] SerializeDictionaryA(Dictionary<ulong, EntityHeader> dictA)
    {
        byte[] buffer = new byte[dictA.Count * ENTRY_A_SIZE];
        int i = 0;
        foreach (var kvp in dictA)
        {
            byte[] keyBytes = Primitives.GetBytes(kvp.Key);
            byte[] valueBytes = Structures.GetBytes(new EntityHeaderCompressed(kvp.Value));

            Buffer.BlockCopy(keyBytes, 0, buffer, 0 + i * ENTRY_A_SIZE, sizeof(ulong));
            Buffer.BlockCopy(valueBytes, 0, buffer, sizeof(ulong) + i * ENTRY_A_SIZE, EntityHeaderCompressed.SIZE);

            i++;
        }
        return buffer;
    }

    private static byte[] SerializeDictionaryB(Dictionary<ulong, uint> dictB)
    {
        byte[] buffer = new byte[dictB.Count * ENTRY_B_SIZE];
        int i = 0;
        foreach (var kvp in dictB)
        {
            byte[] keyBytes = Primitives.GetBytes(kvp.Key);
            byte[] valueBytes = Primitives.GetBytes(kvp.Value);

            Buffer.BlockCopy(keyBytes, 0, buffer, 0 + i * ENTRY_B_SIZE, sizeof(ulong));
            Buffer.BlockCopy(valueBytes, 0, buffer, sizeof(ulong) + i * ENTRY_B_SIZE, sizeof(uint));

            i++;
        }
        return buffer;
    }

    protected override bool IsValid()
    {
        return Bytes.Length >= HEADER_SIZE &&
               Bytes.Length == HEADER_SIZE + (int)EntityLength * ENTRY_A_SIZE + (int)PlayerFixedLength * ENTRY_B_SIZE;
    }
}
