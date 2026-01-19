using System;
using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Core.Binary;

namespace Larnix.Packets.Game
{
    public class BlockUpdate : Payload
    {
        public class Record
        {
            public Vec2Int POS;
            public BlockData2 Block;

            public byte[] Serialize()
            {
                return ArrayUtils.MegaConcat(
                    EndianUnsafe.GetBytes(POS.x),
                    EndianUnsafe.GetBytes(POS.y),
                    Block.Serialize()
                    );
            }

            public static Record Deserialize(byte[] bytes, int offset = 0)
            {
                return new Record
                {
                    POS = new Vec2Int(
                        EndianUnsafe.FromBytes<int>(bytes, 0 + offset),
                        EndianUnsafe.FromBytes<int>(bytes, 4 + offset)),

                    Block = BlockData2.Deserialize(bytes, 8 + offset)
                };
            }
        }

        private const int ENTRY_SIZE = 13;
        private const int MAX_RECORDS = 105;

        public Record[] BlockUpdates => GetRecords(); // n * 13B

        public BlockUpdate() { }
        public BlockUpdate(Record[] records, byte code = 0)
        {
            if (records == null) records = new Record[0];

            byte[] recordBytes = new byte[records.Length * ENTRY_SIZE];
            for (int i = 0; i < records.Length; i++)
            {
                byte[] data = records[i].Serialize();
                Buffer.BlockCopy(data, 0, recordBytes, i * ENTRY_SIZE, ENTRY_SIZE);
            }

            InitializePayload(recordBytes, code);
        }

        public static List<BlockUpdate> CreateList(Record[] records, byte code = 0)
        {
            if (records == null) records = new Record[0];

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
                records[i] = Record.Deserialize(Bytes, i * ENTRY_SIZE);
            }
            return records;
        }

        protected override bool IsValid()
        {
            return Bytes != null &&
                   Bytes.Length <= MAX_RECORDS * ENTRY_SIZE &&
                   Bytes.Length % ENTRY_SIZE == 0;
        }
    }
}
