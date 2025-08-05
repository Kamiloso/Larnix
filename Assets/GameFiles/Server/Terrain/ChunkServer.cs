using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using System;
using System.Linq;
using Larnix.Physics;
using Larnix.Modules.Blocks;

namespace Larnix.Server.Terrain
{
    public class ChunkServer : IDisposable
    {
        public Vector2Int Chunkpos { get; private set; }
        private readonly BlockServer[,] BlocksFront = new BlockServer[16, 16];
        private readonly BlockServer[,] BlocksBack = new BlockServer[16, 16];
        private readonly Dictionary<Vector2Int, StaticCollider> StaticColliders = new();

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

            References.PhysicsManager.SetChunkActive(Chunkpos, true);
        }

        public void PreExecuteFrame() // 1.
        {
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    BlocksBack[x, y].PreFrameTrigger();
                    BlocksFront[x, y].PreFrameTrigger();
                }
        }

        public void ExecuteFrameRandom() // 2.
        {
            foreach (var pos in GetShuffledPositions(16, 16))
            {
                BlocksBack[pos.x, pos.y].FrameUpdateRandom();
                BlocksFront[pos.x, pos.y].FrameUpdateRandom();
            }
        }

        public void ExecuteFrameSequential() // 3.
        {
            // Back update
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    BlocksBack[x, y].FrameUpdateSequential();

            // Front update
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    BlocksFront[x, y].FrameUpdateSequential();
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

            RefreshCollider(pos);

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

            RefreshCollider(pos);
        }

        private void RefreshCollider(Vector2Int pos)
        {
            if(StaticColliders.TryGetValue(pos, out var statCollider))
            {
                References.PhysicsManager.RemoveColliderByReference(statCollider);
                StaticColliders.Remove(pos);
            }

            BlockServer blockServer = BlocksFront[pos.x, pos.y];
            IHasCollider iface = blockServer as IHasCollider;
            if(iface != null)
            {
                StaticCollider staticCollider = iface.STATIC_CreateStaticCollider(blockServer.BlockData.Variant);
                staticCollider.MakeOffset(ChunkMethods.GlobalBlockCoords(Chunkpos, pos));
                StaticColliders[pos] = staticCollider;
                References.PhysicsManager.AddCollider(staticCollider);
            }
        }

        private static List<(int x, int y)> GetShuffledPositions(int width, int height)
        {
            int total = width * height;
            int[] indices = Enumerable.Range(0, total).ToArray();

            var rng = new System.Random();
            for (int i = total - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            var result = new List<(int x, int y)>(total);
            foreach (int idx in indices)
            {
                int x = idx % width;
                int y = idx / width;
                result.Add((x, y));
            }

            return result;
        }

        public void Dispose()
        {
            foreach(StaticCollider staticCollider in StaticColliders.Values)
            {
                References.PhysicsManager.RemoveColliderByReference(staticCollider);
            }

            References.PhysicsManager.SetChunkActive(Chunkpos, false);
            References.BlockDataManager.DisableChunkReference(Chunkpos);
        }
    }
}
