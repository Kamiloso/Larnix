using UnityEngine;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using System.Linq;
using Larnix.Core.Utils;
using Larnix.Blocks;

namespace Larnix.Client.Terrain
{
    public class BasicGridManager : MonoBehaviour
    {
        [SerializeField] protected ChunkedTilemap ChunkedTilemap;

        protected readonly Dictionary<Vec2Int, BlockData2[,]> Chunks = new();
        protected readonly HashSet<Vec2Int> DirtyChunks = new();
        protected readonly HashSet<Vec2Int> VisibleChunks = new();

        protected bool IsMenu => this is not GridManager;
        private GridManager NotMenu => this as GridManager;

        protected virtual void Awake()
        {
            if (IsMenu)
                Ref.BasicGridManager = this;
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
            Chunks[chunk] = BlockArray;
            if (!IsMenu) NotMenu.UpdateChunkColliders(chunk);
            DirtyChunks.Add(chunk);

            if (instantLoad) RedrawGrid(true);
        }

        public void RemoveChunk(Vec2Int chunk)
        {
            if (!Chunks.ContainsKey(chunk))
                return;

            Chunks.Remove(chunk);
            if (!IsMenu) NotMenu.UpdateChunkColliders(chunk);
            DirtyChunks.Add(chunk);

            if (!IsMenu) NotMenu.UnlockChunk(chunk);
        }

        public bool ChunkLoaded(Vec2Int chunk)
        {
            return Chunks.ContainsKey(chunk);
        }

        public void RedrawGrid(bool instantLoad = false)
        {
            // Ascending - ADD
            List<Vec2Int> addChunks = DirtyChunks.Where(c => Chunks.ContainsKey(c)).ToList();
            addChunks.Sort((Vec2Int a, Vec2Int b) => ChunkDistance(a) - ChunkDistance(b));
            foreach (var chunk in addChunks)
            {
                RedrawChunk(chunk, true);
                DirtyChunks.Remove(chunk);
                if(!instantLoad) return; // only one per frame
            }

            // Descending - REMOVE
            List<Vec2Int> removeChunks = DirtyChunks.Where(c => !Chunks.ContainsKey(c)).ToList();
            removeChunks.Sort((Vec2Int a, Vec2Int b) => ChunkDistance(b) - ChunkDistance(a));
            foreach (var chunk in removeChunks)
            {
                RedrawChunk(chunk, false);
                DirtyChunks.Remove(chunk);
                if(!instantLoad) return; // only one per frame
            }
        }

        private void RedrawChunk(Vec2Int chunk, bool active)
        {
            ChunkedTilemap.RedrawChunk(chunk, active ? Chunks[chunk] : null);

            if (active) VisibleChunks.Add(chunk);
            else VisibleChunks.Remove(chunk);
        }

        protected void RedrawTileChecked(Vec2Int chunk, Vec2Int pos, BlockData2 block)
        {
            if(VisibleChunks.Contains(chunk))
            {
                ChunkedTilemap.RedrawExistingTile(chunk, pos, block, true);
            }
        }

        public BlockData2 BlockDataAtPOS(Vec2Int POS)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
            if (!Chunks.ContainsKey(chunk))
                return null;
            
            Vec2Int pos = BlockUtils.LocalBlockCoords(POS);
            return Chunks[chunk][pos.x, pos.y];
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
                Vec2Int playerChunk = BlockUtils.CoordsToChunk(Ref.MainPlayer.Position);
                return GeometryUtils.ManhattanDistance(playerChunk, chunk);
            }
            else
            {
                return chunk.x; // arbitrary distance metric
            }
        }
    }
}
