using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Larnix.Blocks;
using System.Linq;

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
                tile = ConstructTile(block, isFront);
                TileCache.Add(string_id, tile);
            }
            return tile;
        }
        public static Tile GetTileBorder(byte mask)
        {
            string string_id = "mask;" + mask;

            if (!TileCache.TryGetValue(string_id, out Tile tile))
            {
                tile = ConstructTileBorder(mask);
                TileCache.Add(string_id, tile);
            }
            return tile;
        }

        public static Sprite GetSprite(BlockData1 item, bool isFront) =>
            GetTile(item, isFront).sprite;
        public static Sprite GetSpriteBorder(byte mask) =>
            GetTileBorder(mask).sprite;

        public static Texture2D GetTexture(BlockData1 item, bool isFront) =>
            GetTile(item, isFront).sprite.texture;
        public static Texture2D GetTextureBorder(byte mask) =>
            GetTileBorder(mask).sprite.texture;

        private static Tile ConstructTile(BlockData1 block, bool isFront)
        {
            Texture2D texture = Resources.GetTileTexture(block.ID, block.Variant);
            return _ConstructTileUniversal(texture);
        }

        private static Tile ConstructTileBorder(byte mask)
        {
            Texture2D texture = Resources.GenerateBorderTexture(16, mask);
            return _ConstructTileUniversal(texture);
        }

        private static Tile _ConstructTileUniversal(Texture2D texture)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                System.Math.Max(texture.width, texture.height)
            );

            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.None;

            return tile;
        }
    }
}
