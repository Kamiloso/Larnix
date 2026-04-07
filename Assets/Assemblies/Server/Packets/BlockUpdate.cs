using System;
using System.Collections.Generic;
using Larnix.Socket.Packets;
using Larnix.Server.Packets.Structs;
using Larnix.Core;

namespace Larnix.Server.Packets;

public sealed class BlockUpdate : Payload
{
    private static int ENTRY_SIZE => Binary<BlockUpdateRecord>.Size;
    private static int HEADER_SIZE => 0;
    private static int MAX_RECORDS => (1400 - HEADER_SIZE) / ENTRY_SIZE;

    public BlockUpdateRecord[] BlockUpdates => GetRecords(); // n * ENTRY_SIZE

    private BlockUpdate(BlockUpdateRecord[] records, byte code = 0)
    {
        records ??= Array.Empty<BlockUpdateRecord>();

        byte[] recordBytes = new byte[records.Length * ENTRY_SIZE];
        for (int i = 0; i < records.Length; i++)
        {
            byte[] data = Binary<BlockUpdateRecord>.Serialize(records[i]);
            Buffer.BlockCopy(data, 0, recordBytes, i * ENTRY_SIZE, ENTRY_SIZE);
        }

        InitializePayload(recordBytes, code);
    }

    public static List<BlockUpdate> CreateList(BlockUpdateRecord[] records, byte code = 0)
    {
        if (records == null)
            records = new BlockUpdateRecord[0];

        List<BlockUpdate> result = new();

        int eyes = 0;
        while (eyes < records.Length)
        {
            BlockUpdateRecord[] add = records[eyes..Math.Min(records.Length, eyes + MAX_RECORDS)];
            result.Add(new BlockUpdate(add, code));
            eyes += MAX_RECORDS;
        }

        return result;
    }

    private BlockUpdateRecord[] GetRecords()
    {
        BlockUpdateRecord[] records = new BlockUpdateRecord[Bytes.Length / ENTRY_SIZE];
        for (int i = 0; i < records.Length; i++)
        {
            records[i] = Binary<BlockUpdateRecord>.Deserialize(Bytes, i * ENTRY_SIZE);
        }
        return records;
    }

    protected override bool IsValid()
    {
        return Bytes.Length >= HEADER_SIZE &&
               Bytes.Length % ENTRY_SIZE == 0;
    }
}
