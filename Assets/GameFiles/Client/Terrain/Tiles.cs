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
        private const int MAX_CACHE = 512;

        public static Tile GetTile(BlockData1 block, bool isFront)
        {
            string string_id = TileStringID(block, isFront);
            
            if(TileCache.ContainsKey(string_id))
                return TileCache[string_id];

            if(TileCache.Count > MAX_CACHE)
                TileCache.Clear();
            
            Tile tile = ConstructTile(block, isFront);
            TileCache[string_id] = tile;

            return tile;
        }

        public static Sprite GetSprite(BlockData1 item, bool isFront)
        {
            return GetTile(item, isFront).sprite;
        }

        private static string TileStringID(BlockData1 block, bool isFront)
        {
            return block.ID + ":" + block.Variant + ":" + isFront;
        }

        private static Tile ConstructTile(BlockData1 block, bool isFront)
        {
            Texture2D texture = UnityEngine.Resources.Load<Texture2D>("BlockTextures/" + block.ID.ToString() + "-" + block.Variant);
            if (texture != null)
                goto texture_ready;

            texture = UnityEngine.Resources.Load<Texture2D>("BlockTextures/" + block.ID.ToString());
            if (texture != null)
                goto texture_ready;

            texture = UnityEngine.Resources.Load<Texture2D>("BlockTextures/Unknown");
            if (texture == null)
                throw new System.NotImplementedException("Couldn't find Unknown texture!");

            texture_ready:

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                16
            );

            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.None;

            return tile;
        }
    }
}
