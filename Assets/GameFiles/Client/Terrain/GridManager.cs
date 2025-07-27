using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Larnix.Blocks;
using System.Linq;

namespace Larnix.Client.Terrain
{
    public class GridManager : MonoBehaviour
    {
        [SerializeField] Tilemap Tilemap;

        private readonly Dictionary<Vector2Int, BlockData[,]> Chunks = new();
        private readonly HashSet<Vector2Int> VisibleChunks = new();
        private readonly HashSet<Vector2Int> DirtyChunks = new();

        private void Awake()
        {
            References.GridManager = this;
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
        }

        public void RedrawGrid()
        {
            foreach(var chunk in DirtyChunks.ToList())
            {
                bool active = Chunks.ContainsKey(chunk);

                for (int x = 0; x < 16; x++)
                    for (int y = 0; y < 16; y++)
                    {
                        Vector2Int POS = ChunkMethods.GlobalBlockCoords(chunk, new Vector2Int(x, y));
                        Tilemap.SetTile(new Vector3Int(POS.x, POS.y, -1), active ? Tiles.GetTile(Chunks[chunk][x, y].Back, false) : null);
                        Tilemap.SetTile(new Vector3Int(POS.x, POS.y, 0), active ? Tiles.GetTile(Chunks[chunk][x, y].Front, true) : null);
                    }

                DirtyChunks.Remove(chunk);

                if (active) VisibleChunks.Add(chunk);
                else VisibleChunks.Remove(chunk);

                break; // only one redraw per frame (there are better places for simple updates)
            }
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
    }
}
