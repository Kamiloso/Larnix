using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Linq;
using Larnix.Blocks;
using Larnix.Server.Terrain;

namespace Larnix.Socket.Commands
{
    public class ChunkInfo : BaseCommand
    {
        public override Name ID => Name.ChunkInfo;
        public const int BASE_SIZE = 2 * sizeof(int);
        public const int SIZE = BASE_SIZE + 16 * 16 * 5 * sizeof(byte);

        public Vector2Int Chunkpos { get; private set; } // 4B + 4B
        public BlockData[,] Blocks { get; private set; } // (16 * 16 * 5)B

        public ChunkInfo(Vector2Int chunkpos, BlockData[,] blocks, byte code = 0)
            : base(Name.None, code)
        {
            Chunkpos = chunkpos;
            Blocks = blocks;

            DetectDataProblems();
        }

        public ChunkInfo(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || (bytes.Length != SIZE && bytes.Length != BASE_SIZE)) {
                HasProblems = true;
                return;
            }

            bool removes = bytes.Length == BASE_SIZE;

            Chunkpos = new Vector2Int(
                BitConverter.ToInt32(bytes, 0),
                BitConverter.ToInt32(bytes, 4)
                );

            if(removes)
            {
                Blocks = null;
            }
            else
            {
                Blocks = new BlockData[16, 16];
                for (int x = 0; x < 16; x++)
                    for (int y = 0; y < 16; y++)
                    {
                        int ind_s = BASE_SIZE + 5 * (x * 16 + y);
                        int ind_e = ind_s + 5;

                        BlockData block = new();
                        block.DeserializeBaseData(bytes[ind_s..ind_e]);

                        Blocks[x, y] = block;
                    }
            }

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = new byte[Blocks != null ? SIZE : BASE_SIZE];

            Buffer.BlockCopy(BitConverter.GetBytes(Chunkpos.x), 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(Chunkpos.y), 0, bytes, 4, 4);

            if(Blocks != null)
            {
                for (int x = 0; x < 16; x++)
                    for (int y = 0; y < 16; y++)
                    {
                        int ind_s = BASE_SIZE + 5 * (x * 16 + y);
                        byte[] serialized = Blocks[x, y].SerializeBaseData();

                        Buffer.BlockCopy(serialized, 0, bytes, ind_s, 5);
                    }
            }

            return new Packet((byte)ID, Code, bytes);
        }

        protected override void DetectDataProblems()
        {
            bool ok = (
                Chunkpos.x >= ChunkMethods.MIN_CHUNK && Chunkpos.x <= ChunkMethods.MAX_CHUNK &&
                Chunkpos.y >= ChunkMethods.MIN_CHUNK && Chunkpos.y <= ChunkMethods.MAX_CHUNK &&
                (Blocks == null || (Blocks.GetLength(0) == 16 && Blocks.GetLength(1) == 16))
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
