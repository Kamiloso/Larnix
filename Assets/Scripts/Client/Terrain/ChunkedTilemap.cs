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

        private readonly Dictionary<Vec2Int, (Tilemap Front, Tilemap Back)> TileChunks = new();
        private Vec2Int CurrentOrigin = new Vec2Int(0, 0);

        private bool IsMenu;

        private void Awake()
        {
            IsMenu = gameObject.scene.name == "Menu";
        }

        public void RedrawChunk(Vec2Int chunk, BlockData2[,] blocks)
        {
            PrepareChunk(chunk, blocks != null);

            if (blocks == null)
                return;

            Tilemap Front = TileChunks[chunk].Front;
            Tilemap Back = TileChunks[chunk].Back;

            foreach (Vec2Int pos in ChunkIterator.IterateXY())
            {
                int x = pos.x;
                int y = pos.y;

                Vector3Int tilePos = new Vector3Int(x, y, 0);
                Front.SetTile(tilePos, Tiles.GetTile(blocks[x, y].Front, true));
                Back.SetTile(tilePos, Tiles.GetTile(blocks[x, y].Back, false));
            }
        }

        public void RedrawExistingTile(Vec2Int chunk, Vec2Int pos, BlockData2 block)
        {
            Tilemap Front = TileChunks[chunk].Front;
            Tilemap Back = TileChunks[chunk].Back;

            Front.SetTile(pos.ToUnity3(), Tiles.GetTile(block.Front, true));
            Back.SetTile(pos.ToUnity3(), Tiles.GetTile(block.Back, false));
        }

        private void PrepareChunk(Vec2Int chunk, bool hasData)
        {
            bool inMemory = TileChunks.ContainsKey(chunk);

            if(!hasData && inMemory)
                RemoveChunk(chunk);

            if (hasData && !inMemory)
                AddChunk(chunk);
        }

        private void AddChunk(Vec2Int chunk)
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

        private void RemoveChunk(Vec2Int chunk)
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
                Vec2Int newOrigin = Ref.MainPlayer.Position.ExtractSector();
                if (CurrentOrigin != newOrigin)
                {
                    CurrentOrigin = newOrigin;
                    foreach (var vkp in TileChunks)
                    {
                        Vec2Int chunk = vkp.Key;
                        Tilemap frontmap = vkp.Value.Front;
                        Tilemap backmap = vkp.Value.Back;

                        Vector2 realPos = WorldPositionFromOrigin(chunk, CurrentOrigin);
                        frontmap.transform.position = realPos;
                        backmap.transform.position = realPos;
                    }
                }
            }
        }

        private Vector2 WorldPositionFromOrigin(Vec2Int chunk, Vec2Int origin)
        {
            Vec2Int startBlock = BlockUtils.GlobalBlockCoords(chunk, new Vec2Int(0, 0));
            Vector2 subtract = IsMenu ? Vector2.zero : VectorExtensions.ORIGIN_STEP / 2 * Vector2.one;
            return (startBlock - VectorExtensions.ORIGIN_STEP * CurrentOrigin).ToUnity() - subtract;
        }
    }
}
