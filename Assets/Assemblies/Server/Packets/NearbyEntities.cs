using System;
using System.Collections.Generic;
using Larnix.Socket.Packets;
using Larnix.Core.Utils;
using Larnix.Core;

namespace Larnix.Server.Packets;

public sealed class NearbyEntities : Payload
{
    private static int HEADER_SIZE => sizeof(uint) + 2 * sizeof(ushort);
    private static int ENTRY_SIZE => sizeof(ulong);
    private static int MAX_RECORDS => (1400 - HEADER_SIZE) / (2 * ENTRY_SIZE);

    public uint FixedFrame => Binary<uint>.Deserialize(Bytes, 0); // sizeof(uint)
    public ushort AddLength => Binary<ushort>.Deserialize(Bytes, 4); // sizeof(ushort)
    public ushort RemoveLength => Binary<ushort>.Deserialize(Bytes, 6); // sizeof(ushort)
    public ulong[] AddEntities => Binary<ulong>.DeserializeArray(Bytes, AddLength, HEADER_SIZE); // n * ENTRY_SIZE
    public ulong[] RemoveEntities => Binary<ulong>.DeserializeArray(Bytes, RemoveLength, HEADER_SIZE + AddLength * ENTRY_SIZE); // n * ENTRY_SIZE

    private NearbyEntities(uint fixedFrame, ulong[] addEntities, ulong[] removeEntities, byte code = 0)
    {
        addEntities ??= Array.Empty<ulong>();
        removeEntities ??= Array.Empty<ulong>();

        InitializePayload(ArrayUtils.MegaConcat(
            Binary<uint>.Serialize(fixedFrame),
            Binary<ushort>.Serialize((ushort)addEntities.Length),
            Binary<ushort>.Serialize((ushort)removeEntities.Length),
            Binary<ulong>.SerializeArray(addEntities),
            Binary<ulong>.SerializeArray(removeEntities)
            ), code);
    }

    public static NearbyEntities CreateBootstrap(uint fixedFrame)
    {
        return new NearbyEntities(
            fixedFrame,
            Array.Empty<ulong>(),
            Array.Empty<ulong>()
            );
    }

    public static List<NearbyEntities> CreateList(uint fixedFrame, ulong[] addEntities, ulong[] removeEntities, byte code = 0)
    {
        addEntities ??= Array.Empty<ulong>();
        removeEntities ??= Array.Empty<ulong>();

        List<NearbyEntities> result = new();

        int eyes = 0;
        while (eyes < addEntities.Length || eyes < removeEntities.Length)
        {
            ulong[] add = eyes < addEntities.Length ?
                addEntities[eyes..Math.Min(addEntities.Length, eyes + MAX_RECORDS)] :
                Array.Empty<ulong>();

            ulong[] remove = eyes < removeEntities.Length ?
                removeEntities[eyes..Math.Min(removeEntities.Length, eyes + MAX_RECORDS)] :
                Array.Empty<ulong>();

            result.Add(new NearbyEntities(fixedFrame, add, remove, code));
            eyes += MAX_RECORDS;
        }

        return result;
    }

    protected override bool IsValid()
    {
        return Bytes.Length >= HEADER_SIZE &&
               Bytes.Length == HEADER_SIZE + ((int)AddLength + (int)RemoveLength) * ENTRY_SIZE;
    }
}
