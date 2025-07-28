using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Larnix.Blocks;
using System.Linq;
using System;
using Unity.VisualScripting;

namespace Larnix.Client.Terrain
{
    public class GridManager : MonoBehaviour
    {
        [SerializeField] Tilemap TilemapFront;
        [SerializeField] Tilemap TilemapBack;

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
            List<Vector2Int> sortedChunks;

            // Ascending - ADD
            sortedChunks = DirtyChunks.ToList();
            sortedChunks.Sort((Vector2Int a, Vector2Int b) => ChunkDistance(a) - ChunkDistance(b));
            foreach (var chunk in sortedChunks)
            {
                if (!Chunks.ContainsKey(chunk))
                    continue;

                RedrawChunk(chunk);
                DirtyChunks.Remove(chunk);

                return; // only one per frame
            }

            // Descending - REMOVE
            sortedChunks = DirtyChunks.ToList();
            sortedChunks.Sort((Vector2Int a, Vector2Int b) => ChunkDistance(b) - ChunkDistance(a));
            foreach (var chunk in sortedChunks)
            {
                if (Chunks.ContainsKey(chunk))
                    continue;

                RedrawChunk(chunk);
                DirtyChunks.Remove(chunk);

                return; // only one per frame
            }
        }

        private void RedrawChunk(Vector2Int chunk)
        {
            bool active = Chunks.ContainsKey(chunk);

            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    Vector2Int POS = ChunkMethods.GlobalBlockCoords(chunk, new Vector2Int(x, y));
                    TilemapFront.SetTile(new Vector3Int(POS.x, POS.y, 0), active ? Tiles.GetTile(Chunks[chunk][x, y].Front, true) : null);
                    TilemapBack.SetTile(new Vector3Int(POS.x, POS.y, 0), active ? Tiles.GetTile(Chunks[chunk][x, y].Back, false) : null);
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
    }
}
