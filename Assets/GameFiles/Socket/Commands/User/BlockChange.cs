using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Linq;
using Larnix.Socket.Channel;
using Larnix.Blocks;

namespace Larnix.Socket.Commands
{
    public class BlockChange : BaseCommand
    {
        public const int SIZE = 2 * sizeof(int) + 2 * sizeof(ushort) + 3 * sizeof(byte) + 1 * sizeof(long);

        public Vector2Int BlockPosition { get; private set; } // 4B + 4B
        public SingleBlockData Item { get; private set; } // 2B + 1B
        public SingleBlockData Tool { get; private set; } // 2B + 1B
        public byte Front { get; private set; } // 1B
        public long Operation { get; private set; } // 8B

        public BlockChange(Vector2Int blockPosition, SingleBlockData item, SingleBlockData tool, byte front, long operation, byte code = 0)
            : base(code)
        {
            BlockPosition = blockPosition;
            Item = item;
            Tool = tool;
            Front = front;
            Operation = operation;

            DetectDataProblems();
        }

        public BlockChange(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length != SIZE) {
                HasProblems = true;
                return;
            }

            BlockPosition = new Vector2Int(
                EndianUnsafe.FromBytes<int>(bytes, 0),
                EndianUnsafe.FromBytes<int>(bytes, 4)
                );

            Item = new SingleBlockData
            {
                ID = EndianUnsafe.FromBytes<BlockID>(bytes, 8),
                Variant = bytes[10]
            };

            Tool = new SingleBlockData
            {
                ID = EndianUnsafe.FromBytes<BlockID>(bytes, 11),
                Variant = bytes[13]
            };

            Front = bytes[14];

            Operation = EndianUnsafe.FromBytes<long>(bytes, 15);

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(BlockPosition.x),
                EndianUnsafe.GetBytes(BlockPosition.y),
                EndianUnsafe.GetBytes(Item.ID),
                EndianUnsafe.GetBytes(Item.Variant),
                EndianUnsafe.GetBytes(Tool.ID),
                EndianUnsafe.GetBytes(Tool.Variant),
                EndianUnsafe.GetBytes(Front),
                EndianUnsafe.GetBytes(Operation)
            );

            return new Packet(ID, Code, bytes);
        }

        protected override void DetectDataProblems()
        {
            bool ok = (
                BlockPosition.x >= ChunkMethods.MIN_BLOCK && BlockPosition.x <= ChunkMethods.MAX_BLOCK &&
                BlockPosition.y >= ChunkMethods.MIN_BLOCK && BlockPosition.y <= ChunkMethods.MAX_BLOCK &&
                Item != null && Item.Variant <= 16 &&
                Tool != null && Tool.Variant <= 16 &&
                (Front == 0 || Front == 1)
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
