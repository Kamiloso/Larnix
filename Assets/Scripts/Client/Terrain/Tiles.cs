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
        private static readonly Dictionary<string, Tile> _tileCache = new();

        public static Tile GetTile(BlockData1 block, bool isFront)
        {
            string string_id = block.ID + ":" + block.Variant + ":" + isFront;

            if (!_tileCache.TryGetValue(string_id, out Tile tile))
            {
                tile = Textures.CreateTileObject(block.ID, block.Variant);
                _tileCache.Add(string_id, tile);
            }
            return tile;
        }

        public static Sprite GetSprite(BlockData1 item, bool isFront) =>
            GetTile(item, isFront).sprite;

        public static Texture2D GetTexture(BlockData1 item, bool isFront) =>
            GetTile(item, isFront).sprite.texture;

        public static Tile GetBorderTile(Vec2Int POS, bool isMenu)
        {
            BasicGridManager CurrentGrid = isMenu ? Ref.BasicGridManager : Ref.GridManager;
            BlockData1 block = CurrentGrid.BlockDataAtPOS(POS)?.Front;

            IHasConture iface;
            if (block != null && (iface = BlockFactory.GetSlaveInstance<IHasConture>(block.ID)) != null)
            {
                byte alphaByte = iface.STATIC_GetAlphaByte(block.Variant);
                byte borderByte = CurrentGrid.CalculateBorderByte(POS);

                if (alphaByte != 0)
                {
                    string string_id = "border[" + borderByte + ":" + alphaByte + "]";
                    if (!_tileCache.TryGetValue(string_id, out Tile tile))
                    {
                        tile = Textures.CreateBorderTileObject(borderByte, alphaByte);
                        _tileCache.Add(string_id, tile);
                    }
                    return tile;
                }
            }

            return GetTile(new BlockData1(), true);
        }

        public static Sprite GetBorderSprite(Vec2Int POS, bool isMenu) =>
            GetBorderTile(POS, isMenu).sprite;

        public static Texture2D GetBorderTexture(Vec2Int POS, bool isMenu) =>
            GetBorderTile(POS, isMenu).sprite.texture;
    }
}
