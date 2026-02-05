using System;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Binary;
using Larnix.Socket.Packets;
using Larnix.Packets.Structs;

namespace Larnix.Packets
{
    public sealed class BlockUpdate : Payload
    {
        private const int ENTRY_SIZE = 14;
        private const int MAX_RECORDS = 95;

        public BlockUpdateRecord[] BlockUpdates => GetRecords(); // n * 14B

        public BlockUpdate() { }
        private BlockUpdate(BlockUpdateRecord[] records, byte code = 0)
        {
            if (records == null)
                records = new BlockUpdateRecord[0];

            byte[] recordBytes = new byte[records.Length * ENTRY_SIZE];
            for (int i = 0; i < records.Length; i++)
            {
                byte[] data = Structures.GetBytes(records[i]);
                Buffer.BlockCopy(data, 0, recordBytes, i * ENTRY_SIZE, ENTRY_SIZE);
            }

            InitializePayload(recordBytes, code);
        }

        public static List<BlockUpdate> CreateList(BlockUpdateRecord[] records, byte code = 0)
        {
            if (records == null)
                records = new BlockUpdateRecord[0];

            List<BlockUpdate> result = new(records.Length / MAX_RECORDS + 1);

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
                records[i] = Structures.FromBytes<BlockUpdateRecord>(Bytes, i * ENTRY_SIZE);
            }
            return records;
        }

        protected override bool IsValid()
        {
            return Bytes.Length <= MAX_RECORDS * ENTRY_SIZE &&
                   Bytes.Length % ENTRY_SIZE == 0;
        }
    }
}
