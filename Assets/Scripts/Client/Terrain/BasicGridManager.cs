using UnityEngine;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using System.Linq;
using Larnix.Core.Utils;
using Larnix.Blocks;
using Larnix.Blocks.All;
using Larnix.Core;

namespace Larnix.Client.Terrain
{
    public class BasicGridManager : MonoBehaviour
    {
        [SerializeField] protected ChunkedTilemap ChunkedTilemap;

        protected readonly Dictionary<Vec2Int, BlockData2[,]> _allChunks = new();
        protected readonly HashSet<Vec2Int> _dirtyChunks = new();
        protected readonly HashSet<Vec2Int> _visibleChunks = new();

        protected bool IsMenu => this is not GridManager;
        private GridManager NotMenu => this as GridManager;
        protected MainPlayer MainPlayer => !IsMenu ? GlobRef.Get<MainPlayer>() : null;

        protected virtual void Awake()
        {
            GlobRef.Set(this);
        }

        protected virtual void Update()
        {
            ;
        }

        protected virtual void LateUpdate()
        {
            RedrawGrid();
        }

        public void AddChunk(Vec2Int chunk, BlockData2[,] BlockArray, bool instantLoad = false)
        {
            _allChunks[chunk] = BlockArray;
            if (!IsMenu) NotMenu.UpdateChunkColliders(chunk);
            _dirtyChunks.Add(chunk);

            if (instantLoad) RedrawGrid(true);
        }

        public void RemoveChunk(Vec2Int chunk)
        {
            if (!_allChunks.ContainsKey(chunk))
                return;

            _allChunks.Remove(chunk);
            if (!IsMenu) NotMenu.UpdateChunkColliders(chunk);
            _dirtyChunks.Add(chunk);

            if (!IsMenu) NotMenu.UnlockChunk(chunk);
        }

        public bool ChunkLoaded(Vec2Int chunk)
        {
            return _allChunks.ContainsKey(chunk);
        }

        public void RedrawGrid(bool instantLoad = false)
        {
            const int MAX_LOAD_PER_FRAME = 1;
            const int MAX_UNLOAD_PER_FRAME = 2;

            // Ascending - ADD
            List<Vec2Int> addChunks = _dirtyChunks.Where(c => _allChunks.ContainsKey(c)).ToList();
            addChunks.Sort((Vec2Int a, Vec2Int b) => ChunkDistance(a) - ChunkDistance(b));
            int loadCount = 0;

            foreach (var chunk in addChunks)
            {
                RedrawChunk(chunk, true);
                _dirtyChunks.Remove(chunk);
                if (!instantLoad)
                {
                    loadCount++;
                    if (loadCount >= MAX_LOAD_PER_FRAME) break;
                }
            }

            // Descending - REMOVE
            List<Vec2Int> removeChunks = _dirtyChunks.Where(c => !_allChunks.ContainsKey(c)).ToList();
            removeChunks.Sort((Vec2Int a, Vec2Int b) => ChunkDistance(b) - ChunkDistance(a));
            int unloadCount = 0;
            
            foreach (var chunk in removeChunks)
            {
                RedrawChunk(chunk, false);
                _dirtyChunks.Remove(chunk);
                if (!instantLoad)
                {
                    unloadCount++;
                    if (unloadCount >= MAX_UNLOAD_PER_FRAME) break;
                }
            }
        }

        private void RedrawChunk(Vec2Int chunk, bool active)
        {
            ChunkedTilemap.RedrawChunk(chunk, active ? _allChunks[chunk] : null);

            if (active) _visibleChunks.Add(chunk);
            else _visibleChunks.Remove(chunk);
        }

        protected void RedrawTileChecked(Vec2Int chunk, Vec2Int pos, BlockData2 block)
        {
            if(_visibleChunks.Contains(chunk))
            {
                ChunkedTilemap.RedrawExistingTile(chunk, pos, block, true);
            }
        }

        public BlockData2 BlockDataAtPOS(Vec2Int POS)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
            if (!_allChunks.ContainsKey(chunk))
                return null;
            
            Vec2Int pos = BlockUtils.LocalBlockCoords(POS);
            return _allChunks[chunk][pos.x, pos.y];
        }

        public byte CalculateBorderByte(Vec2Int POS)
        {
            byte borderByte = 0;

            int i = 0;
            for (int dy = 1; dy >= -1; dy--)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    Vec2Int n_POS = new Vec2Int(POS.x + dx, POS.y + dy);
                    BlockData1 n_block = BlockDataAtPOS(n_POS)?.Front;

                    IHasConture iface;
                    if (n_block != null && (iface = BlockFactory.GetSlaveInstance<IHasConture>(n_block.ID)) != null)
                    {
                        if (iface.STATIC_GetAlphaByte(n_block.Variant) != 0)
                            borderByte = (byte)(borderByte | (1 << i));
                    }

                    i++;
                }

            return borderByte;
        }

        private int ChunkDistance(Vec2Int chunk)
        {
            if (!IsMenu)
            {
                Vec2Int playerChunk = BlockUtils.CoordsToChunk(MainPlayer.Position);
                return GeometryUtils.ManhattanDistance(playerChunk, chunk);
            }
            else
            {
                return chunk.x; // arbitrary distance metric
            }
        }
    }
}
