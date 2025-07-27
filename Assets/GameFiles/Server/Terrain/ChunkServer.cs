using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using System;

namespace Larnix.Server.Terrain
{
    public class ChunkServer
    {
        public Vector2Int Chunkpos { get; private set; }
        private readonly BlockServer[,] BlocksFront = new BlockServer[16, 16];
        private readonly BlockServer[,] BlocksBack = new BlockServer[16, 16];

        public ChunkServer(Vector2Int chunkpos)
        {
            Chunkpos = chunkpos;
            InitializeData();
        }

        public void ExecuteFrame()
        {
            // Pre-frame configure
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++) {
                    BlocksBack[x, y].MarkedToUpdate = true;
                    BlocksFront[x, y].MarkedToUpdate = true;
                }

            // Back update
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    if (BlocksBack[x, y].MarkedToUpdate)
                        BlocksBack[x, y].DoFrameUpdate();
                }

            // Front update
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    if (BlocksFront[x, y].MarkedToUpdate)
                        BlocksFront[x, y].DoFrameUpdate();
                }
        }

        private void InitializeData()
        {
            BlockData[,] givenBlocks = new BlockData[16, 16]; // TEMPORARY INITIALIZATION
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    BlockData blockData = UnityEngine.Random.Range(0, 5) == 0 ?
                        new BlockData(new SingleBlockData { ID = BlockID.Stone }, new()) :
                        new BlockData(new(), new());

                    givenBlocks[x, y] = blockData;
                }

            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    CreateBlock(new Vector2Int(x, y), givenBlocks[x, y]);
                }
        }

        public void FlushData()
        {
            // not implemented yet
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

        public BlockServer SetBlock(Vector2Int pos, bool isFront, SingleBlockData block)
        {
            BlockServer blockObj = BlockServer.ConstructBlockObject(ChunkMethods.GlobalBlockCoords(Chunkpos, pos), block, isFront);

            if (isFront)
            {
                BlocksFront[pos.x, pos.y].DoOnBreak();
                BlocksFront[pos.x, pos.y] = blockObj;
            }
            else
            {
                BlocksBack[pos.x, pos.y].DoOnBreak();
                BlocksBack[pos.x, pos.y] = blockObj;
            }

            return blockObj;
        }

        private void CreateBlock(Vector2Int pos, BlockData blockData)
        {
            BlockServer frontBlock = BlockServer.ConstructBlockObject(ChunkMethods.GlobalBlockCoords(Chunkpos, pos), blockData.Front, true);
            BlocksFront[pos.x, pos.y] = frontBlock;

            BlockServer backBlock = BlockServer.ConstructBlockObject(ChunkMethods.GlobalBlockCoords(Chunkpos, pos), blockData.Back, false);
            BlocksBack[pos.x, pos.y] = backBlock;
        }
    }
}
