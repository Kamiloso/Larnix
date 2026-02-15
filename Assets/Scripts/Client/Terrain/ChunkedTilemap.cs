using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;
using System;

namespace Larnix.Client.Terrain
{
    public class ChunkedTilemap : MonoBehaviour
    {
        private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;
        private MainPlayer MainPlayer => Ref.MainPlayer;

        private GameObject TilemapPrefabBorder => Prefabs.GetPrefab("Tilemaps", "TilemapBorder");
        private GameObject TilemapPrefabFront => Prefabs.GetPrefab("Tilemaps", "TilemapFront");
        private GameObject TilemapPrefabBack => Prefabs.GetPrefab("Tilemaps", "TilemapBack");

        private class Tilemaps
        {
            public Tilemap Front;
            public Tilemap Back;
            public Tilemap Border;
        }

        private bool IsMenu => gameObject.scene.name == "Menu";
        private readonly Dictionary<Vec2Int, Tilemaps> _tileChunks = new();
        private Vec2Int _currentOrigin = new Vec2Int(0, 0);

        public void RedrawChunk(Vec2Int chunk, BlockData2[,] blocks)
        {
            PrepareChunk(chunk, blocks != null);

            if (blocks == null) return;

            ChunkIterator.Iterate((x, y) =>
            {
                var pos = new Vec2Int(x, y);
                RedrawExistingTile(chunk, pos, blocks[pos.x, pos.y], false);
            });

            RedrawBorderTilesInRect(
                BlockUtils.GlobalBlockCoords(chunk, new Vec2Int(0, 0)),
                BlockUtils.GlobalBlockCoords(chunk, new Vec2Int(CHUNK_SIZE - 1, CHUNK_SIZE - 1))
            );
        }

        public void RedrawExistingTile(Vec2Int chunk, Vec2Int pos, BlockData2 block, bool redrawBorder)
        {
            Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, pos);
            Tilemaps Tilemaps = _tileChunks[chunk];

            Tilemaps.Front.SetTile(pos.ToUnity3(), Tiles.GetTile(block.Front, true));
            Tilemaps.Back.SetTile(pos.ToUnity3(), Tiles.GetTile(block.Back, false));
            Tilemaps.Border.SetTile(pos.ToUnity3(), Tiles.GetBorderTile(POS, IsMenu));

            if (redrawBorder)
            {
                RedrawBorderTilesInRect(
                    POS + new Vec2Int(-1, -1),
                    POS + new Vec2Int(1, 1)
                );
            }
        }

        public void RedrawBorderTilesInRect(Vec2Int POS1, Vec2Int POS2)
        {
            Vec2Int MIN = new Vec2Int(System.Math.Min(POS1.x, POS2.x), System.Math.Min(POS1.y, POS2.y));
            Vec2Int MAX = new Vec2Int(System.Math.Max(POS1.x, POS2.x), System.Math.Max(POS1.y, POS2.y));

            for (int x = MIN.x - 1; x <= MAX.x + 1; x++)
                for (int y = MIN.y - 1; y <= MAX.y + 1; y++)
                {
                    Vec2Int POS = new Vec2Int(x, y);

                    Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
                    Vec2Int pos = BlockUtils.LocalBlockCoords(POS);

                    if (_tileChunks.ContainsKey(chunk))
                    {
                        _tileChunks[chunk].Border.SetTile(pos.ToUnity3(), Tiles.GetBorderTile(POS, IsMenu));
                    }
                }
        }

        private void PrepareChunk(Vec2Int chunk, bool hasData)
        {
            bool inMemory = _tileChunks.ContainsKey(chunk);

            if(!hasData && inMemory)
                RemoveChunk(chunk);

            if (hasData && !inMemory)
                AddChunk(chunk);
        }

        private void AddChunk(Vec2Int chunk)
        {
            Vector2 realPos = WorldPositionFromOrigin(chunk, _currentOrigin);

            Transform trnFront = Instantiate(TilemapPrefabFront, realPos, Quaternion.identity).transform;
            Transform trnBack = Instantiate(TilemapPrefabBack, realPos, Quaternion.identity).transform;
            Transform trnBorder = Instantiate(TilemapPrefabBorder, realPos, Quaternion.identity).transform;

            trnFront.name = $"Front [{chunk.x}, {chunk.y}]";
            trnBack.name = $"Back [{chunk.x}, {chunk.y}]";
            trnBorder.name = $"Border [{chunk.x}, {chunk.y}]";

            trnFront.SetParent(transform, true);
            trnBack.SetParent(transform, true);
            trnBorder.SetParent(transform, true);

            _tileChunks[chunk] = new Tilemaps
            {
                Front = trnFront.GetComponent<Tilemap>(),
                Back = trnBack.GetComponent<Tilemap>(),
                Border = trnBorder.GetComponent<Tilemap>()
            };
        }

        private void RemoveChunk(Vec2Int chunk)
        {
            if (_tileChunks.ContainsKey(chunk))
            {
                var pair = _tileChunks[chunk];
                Destroy(pair.Front.gameObject);
                Destroy(pair.Back.gameObject);
                Destroy(pair.Border.gameObject);
                _tileChunks.Remove(chunk);
            }
        }

        private void Update()
        {
            if (!IsMenu)
            {
                // Update chunk position to match the origin
                Vec2Int newOrigin = MainPlayer.Position.ExtractSector();
                if (_currentOrigin != newOrigin)
                {
                    _currentOrigin = newOrigin;
                    foreach (var kvp in _tileChunks)
                    {
                        Vec2Int chunk = kvp.Key;
                        Tilemap frontmap = kvp.Value.Front;
                        Tilemap backmap = kvp.Value.Back;
                        Tilemap bordermap = kvp.Value.Border;
                        
                        Vector2 realPos = WorldPositionFromOrigin(chunk, _currentOrigin);
                        frontmap.transform.position = realPos;
                        backmap.transform.position = realPos;
                        bordermap.transform.position = realPos;
                    }
                }
            }
        }

        private Vector2 WorldPositionFromOrigin(Vec2Int chunk, Vec2Int origin)
        {
            Vec2Int startBlock = BlockUtils.GlobalBlockCoords(chunk, new Vec2Int(0, 0));
            Vector2 subtract = IsMenu ? Vector2.zero : VectorExtensions.ORIGIN_STEP / 2 * Vector2.one;
            return (startBlock - VectorExtensions.ORIGIN_STEP * _currentOrigin).ToUnity() - subtract;
        }
    }
}
