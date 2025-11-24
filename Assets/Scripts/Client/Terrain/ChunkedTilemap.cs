using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;

namespace Larnix.Client.Terrain
{
    public class ChunkedTilemap : MonoBehaviour
    {
        [SerializeField] GameObject TilemapPrefabBorder;
        [SerializeField] GameObject TilemapPrefabFront;
        [SerializeField] GameObject TilemapPrefabBack;

        private readonly Dictionary<Vector2Int, (Tilemap Front, Tilemap Back)> TileChunks = new();
        private Vector2Int CurrentOrigin = new Vector2Int(0, 0);

        private bool IsMenu;

        private void Awake()
        {
            IsMenu = gameObject.scene.name == "Menu";
        }

        public void RedrawChunk(Vector2Int chunk, BlockData2[,] blocks)
        {
            PrepareChunk(chunk, blocks != null);

            if (blocks == null)
                return;

            Tilemap Front = TileChunks[chunk].Front;
            Tilemap Back = TileChunks[chunk].Back;

            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    Front.SetTile(pos, Tiles.GetTile(blocks[x, y].Front, true));
                    Back.SetTile(pos, Tiles.GetTile(blocks[x, y].Back, false));
                }
        }

        public void RedrawExistingTile(Vector2Int chunk, Vector2Int pos, BlockData2 block)
        {
            Tilemap Front = TileChunks[chunk].Front;
            Tilemap Back = TileChunks[chunk].Back;

            Front.SetTile((Vector3Int)pos, Tiles.GetTile(block.Front, true));
            Back.SetTile((Vector3Int)pos, Tiles.GetTile(block.Back, false));
        }

        private void PrepareChunk(Vector2Int chunk, bool hasData)
        {
            bool inMemory = TileChunks.ContainsKey(chunk);

            if(!hasData && inMemory)
                RemoveChunk(chunk);

            if (hasData && !inMemory)
                AddChunk(chunk);
        }

        private void AddChunk(Vector2Int chunk)
        {
            Vector2 realPos = WorldPositionFromOrigin(chunk, CurrentOrigin);

            Transform trnFront = Instantiate(TilemapPrefabFront, realPos, Quaternion.identity).transform;
            Transform trnBack = Instantiate(TilemapPrefabBack, realPos, Quaternion.identity).transform;

            trnFront.name = $"Front [{chunk.x}, {chunk.y}]";
            trnBack.name = $"Back [{chunk.x}, {chunk.y}]";

            trnFront.SetParent(transform, true);
            trnBack.SetParent(transform, true);

            TileChunks[chunk] = (
                trnFront.GetComponent<Tilemap>(),
                trnBack.GetComponent<Tilemap>()
                );
        }

        private void RemoveChunk(Vector2Int chunk)
        {
            if (TileChunks.ContainsKey(chunk))
            {
                var pair = TileChunks[chunk];
                Destroy(pair.Front.gameObject);
                Destroy(pair.Back.gameObject);
                TileChunks.Remove(chunk);
            }
        }

        private void Update()
        {
            // Update chunk position to match the origin
            if (!IsMenu)
            {
                Vector2Int newOrigin = Ref.MainPlayer.Position.ExtractSector();
                if (CurrentOrigin != newOrigin)
                {
                    CurrentOrigin = newOrigin;
                    foreach (var vkp in TileChunks)
                    {
                        Vector2Int chunk = vkp.Key;
                        Tilemap frontmap = vkp.Value.Front;
                        Tilemap backmap = vkp.Value.Back;

                        Vector2 realPos = WorldPositionFromOrigin(chunk, CurrentOrigin);
                        frontmap.transform.position = realPos;
                        backmap.transform.position = realPos;
                    }
                }
            }
        }

        private Vector2 WorldPositionFromOrigin(Vector2Int chunk, Vector2Int origin)
        {
            Vector2Int startBlock = BlockUtils.GlobalBlockCoords(chunk, new Vector2Int(0, 0));
            Vector2 subtract = IsMenu ? Vector2.zero : Vec2.ORIGIN_STEP / 2 * Vector2.one;
            return (startBlock - Vec2.ORIGIN_STEP * CurrentOrigin) - subtract;
        }
    }
}
