using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Larnix.Blocks.Structs;

namespace Larnix.Client.Terrain
{
    public static class Tiles
    {
        private static readonly Dictionary<string, Tile> TileCache = new();

        public static Tile GetTile(BlockData1 block, bool isFront)
        {
            string string_id = block.ID + ":" + block.Variant + ":" + isFront;

            if (!TileCache.TryGetValue(string_id, out Tile tile))
            {
                tile = Resources.GetTile(block.ID, block.Variant);
                TileCache.Add(string_id, tile);
            }
            return tile;
        }

        public static Sprite GetSprite(BlockData1 item, bool isFront) =>
            GetTile(item, isFront).sprite;

        public static Texture2D GetTexture(BlockData1 item, bool isFront) =>
            GetTile(item, isFront).sprite.texture;
    }
}
