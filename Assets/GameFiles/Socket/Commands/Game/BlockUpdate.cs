using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Linq;
using Larnix.Entities;
using Larnix.Blocks;
using Larnix.Client;

namespace Larnix.Socket.Commands
{
    public class BlockUpdate : BaseCommand
    {
        public override Name ID => Name.BlockUpdate;
        public const int BASE_SIZE = 1 * sizeof(ushort);
        public const int ENTRY_SIZE = 2 * sizeof(int) + 5;

        public const int MAX_RECORDS = 105;

        public List<(Vector2Int block, BlockData data)> BlockUpdates { get; private set; } // size[2B] + ENTRIES * 13B

        public BlockUpdate(List<(Vector2Int block, BlockData data)> blockUpdates, byte code = 0)
            : base(Name.None, code)
        {
            BlockUpdates = blockUpdates;

            DetectDataProblems();
        }

        public BlockUpdate(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length < BASE_SIZE) { // too short
                HasProblems = true;
                return;
            }

            ushort size = EndianUnsafe.FromBytes<ushort>(bytes, 0);

            if (bytes.Length != BASE_SIZE + size * ENTRY_SIZE) { // wrong size
                HasProblems = true;
                return;
            }

            BlockUpdates = new();
            for (int i = 0; i < size; i++)
            {
                int pos_s = BASE_SIZE + i * ENTRY_SIZE;

                Vector2Int block = new Vector2Int(
                    EndianUnsafe.FromBytes<int>(bytes, pos_s),
                    EndianUnsafe.FromBytes<int>(bytes, pos_s + 4)
                    );
                BlockData data = new BlockData();
                data.DeserializeBaseData(bytes[(pos_s + 8)..(pos_s + 13)]);

                BlockUpdates.Add((block, data));
            }

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = new byte[BASE_SIZE + BlockUpdates.Count * ENTRY_SIZE];

            Buffer.BlockCopy(EndianUnsafe.GetBytes((ushort)BlockUpdates.Count), 0, bytes, 0, 2);

            int POS = BASE_SIZE;
            
            foreach(var element in BlockUpdates)
            {
                Buffer.BlockCopy(EndianUnsafe.GetBytes(element.block.x), 0, bytes, POS, 4);
                POS += 4;

                Buffer.BlockCopy(EndianUnsafe.GetBytes(element.block.y), 0, bytes, POS, 4);
                POS += 4;

                Buffer.BlockCopy(element.data.SerializeBaseData(), 0, bytes, POS, 5);
                POS += 5;
            }

            return new Packet((byte)ID, Code, bytes);
        }

        protected override void DetectDataProblems()
        {
            bool ok = (
                BlockUpdates != null && BlockUpdates.Count <= MAX_RECORDS
                );
            HasProblems = HasProblems || !ok;

            const int MIN_BLOCK = ChunkMethods.MIN_BLOCK;
            const int MAX_BLOCK = ChunkMethods.MAX_BLOCK;

            foreach (var element in BlockUpdates)
            {
                bool ok2 = (
                    element.block.x >= MIN_BLOCK && element.block.x <= MAX_BLOCK &&
                    element.block.y >= MIN_BLOCK && element.block.y <= MAX_BLOCK
                    );
                HasProblems = HasProblems || !ok;
            }
        }
    }
}
