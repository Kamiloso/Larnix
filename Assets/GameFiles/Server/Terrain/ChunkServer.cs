using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using System;

namespace Larnix.Server.Terrain
{
    public class ChunkServer : IDisposable
    {
        public Vector2Int Chunkpos { get; private set; }
        private readonly BlockServer[,] BlocksFront = new BlockServer[16, 16];
        private readonly BlockServer[,] BlocksBack = new BlockServer[16, 16];

        private readonly BlockData[,] ActiveChunkReference;

        public ChunkServer(Vector2Int chunkpos)
        {
            Chunkpos = chunkpos;
            ActiveChunkReference = References.BlockDataManager.GetChunkReference(Chunkpos);

            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    CreateBlock(new Vector2Int(x, y), ActiveChunkReference[x, y]);
                }
        }

        public void ExecuteFrame()
        {
            // Pre-frame configure
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    BlocksBack[x, y].PreFrameConfigure();
                    BlocksFront[x, y].PreFrameConfigure();
                }

            // Back update
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    BlocksBack[x, y].FrameUpdate();
                }

            // Front update
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    BlocksFront[x, y].FrameUpdate();
                }
        }

        /// <summary>
        /// BlockArray must be an initialized 16 x 16 array containing any existing BlockData objects.
        /// It is recommended to use a pre-allocated static array for performance.
        /// </summary>
        public void MoveChunkIntoArray(BlockData[,] BlockArray)
        {
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    BlockArray[x, y].Front = BlocksFront[x, y].BlockData;
                    BlockArray[x, y].Back = BlocksBack[x, y].BlockData;
                }
        }

        public BlockServer GetBlock(Vector2Int pos, bool isFront)
        {
            return isFront ? BlocksFront[pos.x, pos.y] : BlocksBack[pos.x, pos.y];
        }

        public BlockServer UpdateBlock(Vector2Int pos, bool isFront, SingleBlockData block)
        {
            BlockServer ret = null;

            // Change blocks

            SingleBlockData oldDataBack = ActiveChunkReference[pos.x, pos.y].Back;
            SingleBlockData oldDataFront = ActiveChunkReference[pos.x, pos.y].Front;

            if (isFront)
            {
                UpdateBlockInArray(ref BlocksFront[pos.x, pos.y], block);
                ActiveChunkReference[pos.x, pos.y].Front = block;
                ret = BlocksFront[pos.x, pos.y];
            }
            else
            {
                UpdateBlockInArray(ref BlocksBack[pos.x, pos.y], block);
                ActiveChunkReference[pos.x, pos.y].Back = block;
                ret = BlocksBack[pos.x, pos.y];
            }

            SingleBlockData newDataBack = ActiveChunkReference[pos.x, pos.y].Back;
            SingleBlockData newDataFront = ActiveChunkReference[pos.x, pos.y].Front;

            // Push send update

            if (
                !SingleBlockData.BaseEquals(oldDataBack, newDataBack) ||
                !SingleBlockData.BaseEquals(oldDataFront, newDataFront)
                )
            {
                References.BlockSender.AddBlockUpdate((
                    ChunkMethods.GlobalBlockCoords(Chunkpos, pos),
                    new BlockData(newDataFront, newDataBack)
                    ));
            }

            // Return

            return ret;
        }

        private static void UpdateBlockInArray(ref BlockServer block, SingleBlockData data)
        {
            if (block.BlockData.ID == data.ID)
            {
                block.BlockData = data;
            }
            else
            {
                block = BlockFactory.ConstructBlockObject(block.Position, data, block.IsFront);
            }
        }

        private void CreateBlock(Vector2Int pos, BlockData blockData)
        {
            BlockServer frontBlock = BlockFactory.ConstructBlockObject(ChunkMethods.GlobalBlockCoords(Chunkpos, pos), blockData.Front, true);
            BlocksFront[pos.x, pos.y] = frontBlock;

            BlockServer backBlock = BlockFactory.ConstructBlockObject(ChunkMethods.GlobalBlockCoords(Chunkpos, pos), blockData.Back, false);
            BlocksBack[pos.x, pos.y] = backBlock;
        }

        public void Dispose()
        {
            References.BlockDataManager.DisableChunkReference(Chunkpos);
        }
    }
}
