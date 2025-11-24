using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using System.Linq;
using System;
using Larnix.Core.Utils;
using Larnix.Core.Physics;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Client.Terrain
{
    public class GridManager : MonoBehaviour
    {
        [SerializeField] ChunkedTilemap ChunkedTilemap;

        private readonly Dictionary<Vector2Int, BlockData2[,]> Chunks = new();
        private readonly HashSet<Vector2Int> DirtyChunks = new();
        private readonly HashSet<Vector2Int> VisibleChunks = new();

        private readonly List<BlockLock> LockedBlocks = new();
        private const double BLOCK_LOCK_TIMEOUT = 5.0; // seconds

        private Dictionary<Vector2Int, StaticCollider> BlockColliders = new();

        private bool isMenu;

        private class BlockLock
        {
            public Vector2Int POS;
            public long operation;
            public double timeout = BLOCK_LOCK_TIMEOUT;
        }

        private void Awake()
        {
            isMenu = gameObject.scene.name == "Menu";
            if(!isMenu) Ref.GridManager = this;
        }

        private void Update()
        {
            foreach (var l in LockedBlocks)
                l.timeout -= Time.deltaTime;

            LockedBlocks.RemoveAll(l => l.timeout < 0);
        }

        private void LateUpdate()
        {
            RedrawGrid();
        }

        public void AddChunk(Vector2Int chunk, BlockData2[,] BlockArray, bool instantLoad = false)
        {
            Chunks[chunk] = BlockArray;
            UpdateChunkColliders(chunk);
            DirtyChunks.Add(chunk);

            if (instantLoad)
                RedrawGrid(true);
        }

        public void RemoveChunk(Vector2Int chunk)
        {
            if (!Chunks.ContainsKey(chunk))
                return;

            Chunks.Remove(chunk);
            UpdateChunkColliders(chunk);
            DirtyChunks.Add(chunk);

            UnlockChunk(chunk);
        }

        public bool ChunkLoaded(Vector2Int chunk)
        {
            return Chunks.Keys.Contains(chunk);
        }

        public void UpdateBlock(Vector2Int POS, BlockData2 data, long? unlock = null)
        {
            Vector2Int chunk = BlockUtils.CoordsToChunk(POS);
            Vector2Int pos = BlockUtils.LocalBlockCoords(POS);

            if (unlock != null)
                UnlockBlock((long)unlock);

            if (Chunks.ContainsKey(chunk) && !IsBlockLocked(POS))
                ChangeBlockData(POS, data);
        }

        public void RedrawGrid(bool instantLoad = false)
        {
            // Ascending - ADD
            List<Vector2Int> addChunks = DirtyChunks.Where(c => Chunks.ContainsKey(c)).ToList();
            addChunks.Sort((Vector2Int a, Vector2Int b) => ChunkDistance(a) - ChunkDistance(b));
            foreach (var chunk in addChunks)
            {
                RedrawChunk(chunk, true);
                DirtyChunks.Remove(chunk);
                if(!instantLoad) return; // only one per frame
            }

            // Descending - REMOVE
            List<Vector2Int> removeChunks = DirtyChunks.Where(c => !Chunks.ContainsKey(c)).ToList();
            removeChunks.Sort((Vector2Int a, Vector2Int b) => ChunkDistance(b) - ChunkDistance(a));
            foreach (var chunk in removeChunks)
            {
                RedrawChunk(chunk, false);
                DirtyChunks.Remove(chunk);
                if(!instantLoad) return; // only one per frame
            }
        }

        private void RedrawChunk(Vector2Int chunk, bool active)
        {
            ChunkedTilemap.RedrawChunk(chunk, active ? Chunks[chunk] : null);

            if (active) VisibleChunks.Add(chunk);
            else VisibleChunks.Remove(chunk);
        }

        private int ChunkDistance(Vector2Int chunk)
        {
            return Common.ManhattanDistance(
                BlockUtils.CoordsToChunk(!isMenu ? Ref.MainPlayer.Position : new Vec2(0, 0)),
                chunk
                );
        }

        public bool LoadedAroundPlayer()
        {
            if (isMenu)
                return false;

            HashSet<Vector2Int> nearbyChunks = BlockUtils.GetNearbyChunks(
                BlockUtils.CoordsToChunk(Ref.MainPlayer.Position),
                BlockUtils.LOADING_DISTANCE
                );

            nearbyChunks.ExceptWith(VisibleChunks);
            return nearbyChunks.Count == 0;
        }

        /// <summary>
        /// Warning: Can be null if can't find a block!
        /// </summary>
        public BlockData2 BlockDataAtPOS(Vector2Int POS)
        {
            Vector2Int chunk = BlockUtils.CoordsToChunk(POS);
            if (!Chunks.ContainsKey(chunk))
                return null;
            
            Vector2Int pos = BlockUtils.LocalBlockCoords(POS);
            return Chunks[chunk][pos.x, pos.y];
        }

        public long PlaceBlockClient(Vector2Int POS, BlockData1 block, bool front)
        {
            BlockData2 oldblock = BlockDataAtPOS(POS);
            if (oldblock == null)
                throw new InvalidOperationException($"Cannot place block at {POS}");

            Vector2Int chunk = BlockUtils.CoordsToChunk(POS);
            Vector2Int pos = BlockUtils.LocalBlockCoords(POS);

            BlockData2 blockdata = new BlockData2(
                front ? block : oldblock.Front.DeepCopy(),
                front ? oldblock.Back.DeepCopy() : block
                );

            ChangeBlockData(POS, blockdata);
            long operation = LockBlock(POS);
            return operation;
        }

        public long BreakBlockClient(Vector2Int POS, bool front)
        {
            BlockData2 oldblock = BlockDataAtPOS(POS);
            if (oldblock == null)
                throw new InvalidOperationException($"Cannot break block at {POS}");

            Vector2Int chunk = BlockUtils.CoordsToChunk(POS);
            Vector2Int pos = BlockUtils.LocalBlockCoords(POS);

            BlockData2 blockdata = new BlockData2(
                front ? new BlockData1 { } : oldblock.Front.DeepCopy(),
                front ? oldblock.Back.DeepCopy() : new BlockData1 { }
                );

            ChangeBlockData(POS, blockdata);
            long operation = LockBlock(POS);
            return operation;
        }

        private void ChangeBlockData(Vector2Int POS, BlockData2 block)
        {
            Vector2Int chunk = BlockUtils.CoordsToChunk(POS);
            Vector2Int pos = BlockUtils.LocalBlockCoords(POS);

            Chunks[chunk][pos.x, pos.y] = block;
            RedrawTileChecked(chunk, pos, block);
            UpdateBlockCollider(POS, block);
        }

        private void RedrawTileChecked(Vector2Int chunk, Vector2Int pos, BlockData2 block)
        {
            if(VisibleChunks.Contains(chunk))
                ChunkedTilemap.RedrawExistingTile(chunk, pos, block);
        }

        private void UpdateChunkColliders(Vector2Int chunk)
        {
            if (!Chunks.TryGetValue(chunk, out BlockData2[,] chunkBlocks))
                chunkBlocks = null;

            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    Vector2Int POS = BlockUtils.GlobalBlockCoords(chunk, new Vector2Int(x, y));
                    UpdateBlockCollider(POS, chunkBlocks != null ? chunkBlocks[x, y] : null);
                }

            if(!isMenu) Ref.PhysicsManager.SetChunkActive(chunk, chunkBlocks != null);
        }

        private void UpdateBlockCollider(Vector2Int POS, BlockData2 block)
        {
            if (BlockColliders.TryGetValue(POS, out var statCollider))
            {
                if(!isMenu) Ref.PhysicsManager.RemoveColliderByReference(statCollider);
                BlockColliders.Remove(POS);
            }

            if(block != null)
            {
                IHasCollider iface = BlockFactory.GetSlaveInstance<IHasCollider>(block.Front.ID);
                if (iface != null)
                {
                    StaticCollider statCol = StaticCollider.Create(
                        new Vec2(iface.COLLIDER_WIDTH(), iface.COLLIDER_HEIGHT()),
                        new Vec2(iface.COLLIDER_OFFSET_X(), iface.COLLIDER_OFFSET_Y()),
                        POS
                        );
                    if(!isMenu) Ref.PhysicsManager.AddCollider(statCol);
                    BlockColliders.Add(POS, statCol);
                }
            }
        }

        public bool IsBlockLocked(Vector2Int POS)
        {
            return LockedBlocks.Any(l => l.POS == POS);
        }

        private long LockBlock(Vector2Int POS)
        {
            System.Random Rand = Common.Rand();
            long operation = ((long)Rand.Next(int.MinValue, int.MaxValue) << 32) | (uint)Rand.Next(int.MinValue, int.MaxValue);
            LockedBlocks.Add(new BlockLock
            {
                POS = POS,
                operation = operation,
            });
            return operation;
        }

        private void UnlockBlock(long operation)
        {
            LockedBlocks.RemoveAll(l => l.operation == operation);
        }

        private void UnlockChunk(Vector2Int chunk)
        {
            LockedBlocks.RemoveAll(l => BlockUtils.InChunk(chunk, l.POS));
        }
    }
}
