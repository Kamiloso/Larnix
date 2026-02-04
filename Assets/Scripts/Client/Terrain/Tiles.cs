using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Blocks;

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
                tile = Resources.CreateTileObject(block.ID, block.Variant);
                TileCache.Add(string_id, tile);
            }
            return tile;
        }

        public static Sprite GetSprite(BlockData1 item, bool isFront) =>
            GetTile(item, isFront).sprite;

        public static Texture2D GetTexture(BlockData1 item, bool isFront) =>
            GetTile(item, isFront).sprite.texture;

        public static Tile GetBorderTile(Vec2Int POS)
        {
            BlockData1 block = Ref.GridManager.BlockDataAtPOS(POS)?.Front;

            IHasConture iface;
            if (block != null && (iface = BlockFactory.GetSlaveInstance<IHasConture>(block.ID)) != null)
            {
                byte alphaByte = iface.STATIC_GetAlphaByte(block.Variant);
                byte borderByte = Ref.GridManager.CalculateBorderByte(POS);

                if (alphaByte != 0)
                {
                    string string_id = "border[" + borderByte + ":" + alphaByte + "]";
                    if (!TileCache.TryGetValue(string_id, out Tile tile))
                    {
                        tile = Resources.CreateBorderTileObject(borderByte, alphaByte);
                        TileCache.Add(string_id, tile);
                    }
                    return tile;
                }
            }

            return GetTile(new BlockData1(), true);
        }

        public static Sprite GetBorderSprite(Vec2Int POS) =>
            GetBorderTile(POS).sprite;

        public static Texture2D GetBorderTexture(Vec2Int POS) =>
            GetBorderTile(POS).sprite.texture;
    }
}
