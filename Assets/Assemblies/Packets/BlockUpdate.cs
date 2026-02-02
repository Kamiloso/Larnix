using System;
using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;
using Larnix.Core.Binary;
using Larnix.Socket.Packets;

namespace Larnix.Packets
{
    public sealed class BlockUpdate : Payload
    {
        private const int ENTRY_SIZE = 13;
        private const int MAX_RECORDS = 105;

        public Record[] BlockUpdates => GetRecords(); // n * 13B

        public BlockUpdate() { }
        private BlockUpdate(Record[] records, byte code = 0)
        {
            if (records == null)
                records = new Record[0];

            byte[] recordBytes = new byte[records.Length * ENTRY_SIZE];
            for (int i = 0; i < records.Length; i++)
            {
                byte[] data = Structures.GetBytes(records[i]);
                Buffer.BlockCopy(data, 0, recordBytes, i * ENTRY_SIZE, ENTRY_SIZE);
            }

            InitializePayload(recordBytes, code);
        }

        public static List<BlockUpdate> CreateList(Record[] records, byte code = 0)
        {
            if (records == null)
                records = new Record[0];

            List<BlockUpdate> result = new(records.Length / MAX_RECORDS + 1);

            int eyes = 0;
            while (eyes < records.Length)
            {
                Record[] add = records[eyes..Math.Min(records.Length, eyes + MAX_RECORDS)];
                result.Add(new BlockUpdate(add, code));
                eyes += MAX_RECORDS;
            }

            return result;
        }

        private Record[] GetRecords()
        {
            Record[] records = new Record[Bytes.Length / ENTRY_SIZE];
            for (int i = 0; i < records.Length; i++)
            {
                records[i] = Structures.FromBytes<Record>(Bytes, i * ENTRY_SIZE);
            }
            return records;
        }

        protected override bool IsValid()
        {
            return Bytes.Length <= MAX_RECORDS * ENTRY_SIZE &&
                   Bytes.Length % ENTRY_SIZE == 0;
        }

        public class Record : IBinary<Record>
        {
            public const int SIZE = Vec2Int.SIZE + BlockData2.SIZE;

            public Vec2Int POS { get; private set; }
            public BlockData2 Block { get; private set; }

            public Record() => Block = new();
            public Record(Vec2Int pos, BlockData2 block)
            {
                POS = pos;
                Block = block ?? new();
            }

            public byte[] Serialize()
            {
                return ArrayUtils.MegaConcat(
                    Structures.GetBytes(POS),
                    Structures.GetBytes(Block)
                    );
            }

            public bool Deserialize(byte[] bytes, int offset = 0)
            {
                if (offset + SIZE > bytes.Length)
                    return false;

                POS = Structures.FromBytes<Vec2Int>(bytes, offset);
                offset += Vec2Int.SIZE;

                Block = Structures.FromBytes<BlockData2>(bytes, offset);
                offset += BlockData2.SIZE;

                return true;
            }
        }
    }
}
