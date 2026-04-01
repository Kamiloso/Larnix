using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Larnix.Core.Vectors;
using Larnix.Blocks;
using Larnix.Blocks.All;
using Larnix.Core;
using Larnix.Graphics;
using Larnix.GameCore.Structs;
using Larnix.Client.Terrain;

namespace Larnix.Client.Graphics;

public static class Tiles
{
    private static readonly Dictionary<string, Tile> TileCache = new();

    public static Tile GetTile(BlockHeader1 block, bool isFront)
    {
        string string_id = block.ID + ":" + block.Variant + ":" + isFront;

        if (!TileCache.TryGetValue(string_id, out Tile tile))
        {
            tile = Textures.CreateTileObject(block.ID, block.Variant);
            TileCache.Add(string_id, tile);
        }
        return tile;
    }

    public static Sprite GetSprite(BlockHeader1 item, bool isFront) =>
        GetTile(item, isFront).sprite;

    public static Texture2D GetTexture(BlockHeader1 item, bool isFront) =>
        GetTile(item, isFront).sprite.texture;

    public static Tile GetBorderTile(Vec2Int POS, bool isMenu)
    {
        BasicGridManager grid = isMenu
            ? GlobRef.Get<BasicGridManager>()
            : GlobRef.Get<GridManager>();

        BlockHeader1? blockNullable = grid.BlockDataAtPOS(POS)?.Front;

        IHasConture iface;
        if (blockNullable != null && (iface = BlockFactory.GetSlaveInstance<IHasConture>(blockNullable.Value.ID)) != null)
        {
            BlockHeader1 block = blockNullable.Value;

            byte alphaByte = iface.STATIC_GetAlphaByte(block.Variant);
            byte borderByte = grid.CalculateBorderByte(POS);

            if (alphaByte != 0)
            {
                string string_id = "border[" + borderByte + ":" + alphaByte + "]";
                if (!TileCache.TryGetValue(string_id, out Tile tile))
                {
                    tile = Textures.CreateBorderTileObject(borderByte, alphaByte);
                    TileCache.Add(string_id, tile);
                }
                return tile;
            }
        }

        return GetTile(BlockHeader1.Air, true);
    }

    public static Sprite GetBorderSprite(Vec2Int POS, bool isMenu) =>
        GetBorderTile(POS, isMenu).sprite;

    public static Texture2D GetBorderTexture(Vec2Int POS, bool isMenu) =>
        GetBorderTile(POS, isMenu).sprite.texture;
}
