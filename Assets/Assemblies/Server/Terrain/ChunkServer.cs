using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using System;
using System.Linq;
using Larnix.Core.Physics;

namespace Larnix.Server.Terrain
{
    internal class ChunkServer : IDisposable
    {
        private WorldAPI WorldAPI => Ref.ChunkLoading.WorldAPI;

        public Vector2Int Chunkpos { get; private set; }
        private readonly BlockServer[,] BlocksFront = new BlockServer[16, 16];
        private readonly BlockServer[,] BlocksBack = new BlockServer[16, 16];
        private readonly Dictionary<Vector2Int, StaticCollider> StaticColliders = new();

        private readonly BlockData2[,] ActiveChunkReference;

        public ChunkServer(Vector2Int chunkpos)
        {
            Chunkpos = chunkpos;
            ActiveChunkReference = Ref.BlockDataManager.GetChunkReference(Chunkpos);

            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    CreateBlock(new Vector2Int(x, y), ActiveChunkReference[x, y]);
                }

            Ref.PhysicsManager.SetChunkActive(Chunkpos, true);
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
        public void MoveChunkIntoArray(BlockData2[,] BlockArray)
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

        public BlockServer UpdateBlock(Vector2Int pos, bool isFront, BlockData1 block)
        {
            BlockServer ret = null;

            // Change blocks

            BlockData1 oldDataBack = ActiveChunkReference[pos.x, pos.y].Back;
            BlockData1 oldDataFront = ActiveChunkReference[pos.x, pos.y].Front;

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

            BlockData1 newDataBack = ActiveChunkReference[pos.x, pos.y].Back;
            BlockData1 newDataFront = ActiveChunkReference[pos.x, pos.y].Front;

            // Push send update

            if (
                !BlockData1.BaseEquals(oldDataBack, newDataBack) ||
                !BlockData1.BaseEquals(oldDataFront, newDataFront)
                )
            {
                Ref.BlockSender.AddBlockUpdate((
                    ChunkMethods.GlobalBlockCoords(Chunkpos, pos),
                    new BlockData2(newDataFront, newDataBack)
                    ));
            }

            return ret;
        }

        private void UpdateBlockInArray(ref BlockServer block, BlockData1 data)
        {
            if (BlockData1.BaseEquals(block.BlockData, data))
            {
                // the same block, only NBT has changed
                block.BlockData = data;
            }
            else
            {
                // different block, construct new object (reset all optional metadata)
                block = ConstructAndBindBlockObject(block.Position, data, block.IsFront);
            }
        }

        private void CreateBlock(Vector2Int pos, BlockData2 blockData)
        {
            BlockServer frontBlock = ConstructAndBindBlockObject(ChunkMethods.GlobalBlockCoords(Chunkpos, pos), blockData.Front, true);
            BlocksFront[pos.x, pos.y] = frontBlock;

            BlockServer backBlock = ConstructAndBindBlockObject(ChunkMethods.GlobalBlockCoords(Chunkpos, pos), blockData.Back, false);
            BlocksBack[pos.x, pos.y] = backBlock;

            RefreshCollider(pos);
        }

        private BlockServer ConstructAndBindBlockObject(Vector2Int POS, BlockData1 block, bool front)
        {
            BlockServer blockServer = BlockFactory.ConstructBlockObject(POS, block, front, WorldAPI);
            return blockServer;
        }

        private void RefreshCollider(Vector2Int pos)
        {
            if(StaticColliders.TryGetValue(pos, out var statCollider))
            {
                Ref.PhysicsManager.RemoveColliderByReference(statCollider);
                StaticColliders.Remove(pos);
            }

            BlockServer blockServer = BlocksFront[pos.x, pos.y];
            IHasCollider iface = blockServer as IHasCollider;
            if(iface != null)
            {
                StaticCollider staticCollider = StaticCollider.Create(
                    new Vec2(iface.COLLIDER_WIDTH(), iface.COLLIDER_HEIGHT()),
                    new Vec2(iface.COLLIDER_OFFSET_X(), iface.COLLIDER_OFFSET_Y()),
                    ChunkMethods.GlobalBlockCoords(Chunkpos, pos)
                    );
                StaticColliders[pos] = staticCollider;
                Ref.PhysicsManager.AddCollider(staticCollider);
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
                Ref.PhysicsManager.RemoveColliderByReference(staticCollider);
            }

            Ref.PhysicsManager.SetChunkActive(Chunkpos, false);
            Ref.BlockDataManager.DisableChunkReference(Chunkpos);
        }
    }
}
