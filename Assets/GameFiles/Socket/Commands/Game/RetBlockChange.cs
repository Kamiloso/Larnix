using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Linq;
using Larnix.Blocks;

namespace Larnix.Socket.Commands
{
    public class RetBlockChange : BaseCommand
    {
        public override Name ID => Name.RetBlockChange;
        public const int SIZE = 2 * sizeof(int) + 1 * sizeof(long) + 1 * (5) + 2 * sizeof(byte);

        public Vector2Int BlockPosition { get; private set; } // 4B + 4B
        public long Operation { get; private set; } // 8B
        public BlockData CurrentBlock { get; private set; } // 5B
        public byte Front { get; private set; } // 1B
        public byte Success { get; private set; } // 1B

        public RetBlockChange(Vector2Int blockPosition, long operation, BlockData currentBlock, byte front, byte success, byte code = 0)
            : base(Name.None, code)
        {
            BlockPosition = blockPosition;
            Operation = operation;
            CurrentBlock = currentBlock;
            Front = front;
            Success = success;

            DetectDataProblems();
        }

        public RetBlockChange(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length != SIZE) {
                HasProblems = true;
                return;
            }

            BlockPosition = new Vector2Int(
                BitConverter.ToInt32(bytes, 0),
                BitConverter.ToInt32(bytes, 4)
                );

            Operation = BitConverter.ToInt64(bytes, 8);

            CurrentBlock = new BlockData();
            CurrentBlock.DeserializeBaseData(bytes[16..21]);

            Front = bytes[21];
            Success = bytes[22];

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(BlockPosition.x);
                bw.Write(BlockPosition.y);
                bw.Write(Operation);
                bw.Write(CurrentBlock.SerializeBaseData());
                bw.Write(Front);
                bw.Write(Success);

                return new Packet((byte)ID, Code, ms.ToArray());
            }
        }

        protected override void DetectDataProblems()
        {
            bool ok = (
                BlockPosition.x >= ChunkMethods.MIN_BLOCK && BlockPosition.x <= ChunkMethods.MAX_BLOCK &&
                BlockPosition.y >= ChunkMethods.MIN_BLOCK && BlockPosition.y <= ChunkMethods.MAX_BLOCK &&
                CurrentBlock != null && CurrentBlock.Front != null && CurrentBlock.Back != null &&
                (Front == 0 || Front == 1) && (Success == 0 || Success == 1)
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
