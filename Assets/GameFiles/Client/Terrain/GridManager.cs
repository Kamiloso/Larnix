using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Larnix.Blocks;
using System.Linq;
using System;
using Larnix.Socket.Commands;

namespace Larnix.Client.Terrain
{
    public class GridManager : MonoBehaviour
    {
        [SerializeField] Tilemap TilemapFront;
        [SerializeField] Tilemap TilemapBack;

        private readonly Dictionary<Vector2Int, BlockData[,]> Chunks = new();
        private readonly HashSet<Vector2Int> DirtyChunks = new();
        private readonly HashSet<Vector2Int> VisibleChunks = new();

        private readonly List<BlockLock> LockedBlocks = new();
        private const double BLOCK_LOCK_TIMEOUT = 5.0; // seconds

        private class BlockLock
        {
            public Vector2Int POS;
            public long operation;
            public double timeout = BLOCK_LOCK_TIMEOUT;
        }

        private void Awake()
        {
            References.GridManager = this;
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

        public void AddChunk(Vector2Int chunk, BlockData[,] BlockArray)
        {
            Chunks[chunk] = BlockArray;
            DirtyChunks.Add(chunk);
        }

        public void RemoveChunk(Vector2Int chunk)
        {
            if (!Chunks.ContainsKey(chunk))
                return;

            Chunks.Remove(chunk);
            DirtyChunks.Add(chunk);

            UnlockChunk(chunk);
        }

        public void UpdateBlock(Vector2Int POS, BlockData data, long unlock = 0)
        {
            Vector2Int chunk = ChunkMethods.CoordsToChunk(POS);
            Vector2Int pos = ChunkMethods.LocalBlockCoords(POS);

            if (unlock != 0)
                UnlockBlock(unlock);

            if (Chunks.ContainsKey(chunk) && !IsBlockLocked(POS))
                ChangeBlockData(POS, data);
        }

        public void RedrawGrid()
        {
            // Ascending - ADD
            List<Vector2Int> addChunks = DirtyChunks.Where(c => Chunks.ContainsKey(c)).ToList();
            addChunks.Sort((Vector2Int a, Vector2Int b) => ChunkDistance(a) - ChunkDistance(b));
            foreach (var chunk in addChunks)
            {
                RedrawChunk(chunk, true);
                DirtyChunks.Remove(chunk);
                return; // only one per frame
            }

            // Descending - REMOVE
            List<Vector2Int> removeChunks = DirtyChunks.Where(c => !Chunks.ContainsKey(c)).ToList();
            removeChunks.Sort((Vector2Int a, Vector2Int b) => ChunkDistance(b) - ChunkDistance(a));
            foreach (var chunk in removeChunks)
            {
                RedrawChunk(chunk, false);
                DirtyChunks.Remove(chunk);
                return; // only one per frame
            }
        }

        private void RedrawChunk(Vector2Int chunk, bool active)
        {
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    Vector2Int POS = ChunkMethods.GlobalBlockCoords(chunk, new Vector2Int(x, y));
                    RedrawTileUnchecked(POS, active ? Chunks[chunk][x, y] : null);
                }

            if (active) VisibleChunks.Add(chunk);
            else VisibleChunks.Remove(chunk);
        }

        private int ChunkDistance(Vector2Int chunk)
        {
            return Common.ManhattanDistance(
                ChunkMethods.CoordsToChunk(References.MainPlayer.GetPosition()),
                chunk
                );
        }

        public bool LoadedAroundPlayer()
        {
            HashSet<Vector2Int> nearbyChunks = Server.Terrain.ChunkLoading.GetNearbyChunks(
                ChunkMethods.CoordsToChunk(References.MainPlayer.GetPosition()),
                Server.Terrain.ChunkLoading.LOADING_DISTANCE
                );

            nearbyChunks.ExceptWith(VisibleChunks);
            return nearbyChunks.Count == 0;
        }

        /// <summary>
        /// Warning: Can be null if can't find a block!
        /// </summary>
        public BlockData BlockDataAtPOS(Vector2Int POS)
        {
            Vector2Int chunk = ChunkMethods.CoordsToChunk(POS);
            if (!Chunks.ContainsKey(chunk))
                return null;
            
            Vector2Int pos = ChunkMethods.LocalBlockCoords(POS);
            return Chunks[chunk][pos.x, pos.y];
        }

        public long PlaceBlockClient(Vector2Int POS, SingleBlockData block, bool front)
        {
            BlockData oldblock = BlockDataAtPOS(POS);
            if (oldblock == null)
                throw new InvalidOperationException($"Cannot place block at {POS}");

            Vector2Int chunk = ChunkMethods.CoordsToChunk(POS);
            Vector2Int pos = ChunkMethods.LocalBlockCoords(POS);

            BlockData blockdata = oldblock.ShallowCopy();

            if (front) blockdata.Front = block;
            else blockdata.Back = block;

            ChangeBlockData(POS, blockdata);
            long operation = LockBlock(POS);
            return operation;
        }

        public long BreakBlockClient(Vector2Int POS, bool front)
        {
            BlockData oldblock = BlockDataAtPOS(POS);
            if (oldblock == null)
                throw new InvalidOperationException($"Cannot break block at {POS}");

            Vector2Int chunk = ChunkMethods.CoordsToChunk(POS);
            Vector2Int pos = ChunkMethods.LocalBlockCoords(POS);

            BlockData blockdata = oldblock.ShallowCopy();

            if (front) blockdata.Front = new SingleBlockData { };
            else blockdata.Back = new SingleBlockData { };

            ChangeBlockData(POS, blockdata);
            long operation = LockBlock(POS);
            return operation;
        }

        private void ChangeBlockData(Vector2Int POS, BlockData block)
        {
            Vector2Int chunk = ChunkMethods.CoordsToChunk(POS);
            Vector2Int pos = ChunkMethods.LocalBlockCoords(POS);

            Chunks[chunk][pos.x, pos.y] = block;
            RedrawTileChecked(POS, block);
        }

        private void RedrawTileChecked(Vector2Int POS, BlockData block)
        {
            Vector2Int chunk = ChunkMethods.CoordsToChunk(POS);
            if(VisibleChunks.Contains(chunk))
                RedrawTileUnchecked(POS, block);
        }

        private void RedrawTileUnchecked(Vector2Int POS, BlockData block)
        {
            Tile tileFront = block != null ? Tiles.GetTile(block.Front, true) : null;
            Tile tileBack = block != null ? Tiles.GetTile(block.Back, false) : null;

            TilemapFront.SetTile(new Vector3Int(POS.x, POS.y, 0), tileFront);
            TilemapBack.SetTile(new Vector3Int(POS.x, POS.y, 0), tileBack);
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
            LockedBlocks.RemoveAll(l => ChunkMethods.InChunk(chunk, l.POS));
        }
    }
}
