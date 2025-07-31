using Larnix.Blocks;
using Larnix.Modules.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Server.Terrain
{
    public static class WorldAPI
    {
        public static BlockServer GetBlock(Vector2Int POS, bool isFront)
        {
            Vector2Int chunk = ChunkMethods.CoordsToChunk(POS);

            if (!References.ChunkLoading.TryGetChunk(chunk, out var chunkObject))
                return null;

            Vector2Int pos = ChunkMethods.LocalBlockCoords(POS);
            return chunkObject.GetBlock(pos, isFront);
        }

        public static List<BlockServer> GetBlocksAround(Vector2Int POS, bool isFront)
        {
            List<BlockServer> blocks = new();

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx != 0 || dy != 0) // not center
                        blocks.Add(GetBlock(POS + new Vector2Int(dx, dy), isFront));
                }

            return blocks;
        }

        public static BlockServer UpdateBlockVariant(Vector2Int POS, bool isFront, byte Variant)
        {
            BlockServer oldBlock = GetBlock(POS, isFront);
            if (oldBlock == null)
                return null;

            SingleBlockData data = oldBlock.BlockData.ShallowCopy();
            data.Variant = Variant;
            return UpdateBlock(POS, isFront, data);
        }

        public static BlockServer UpdateBlockNBT(Vector2Int POS, bool isFront, string NBT)
        {
            BlockServer oldBlock = GetBlock(POS, isFront);
            if (oldBlock == null)
                return null;

            SingleBlockData data = oldBlock.BlockData.ShallowCopy();
            data.NBT = NBT;
            return UpdateBlock(POS, isFront, data);
        }

        public static BlockServer UpdateBlock(Vector2Int POS, bool isFront, SingleBlockData blockData)
        {
            Vector2Int chunk = ChunkMethods.CoordsToChunk(POS);

            if (!References.ChunkLoading.TryGetChunk(chunk, out var chunkObject))
                return null;

            Vector2Int pos = ChunkMethods.LocalBlockCoords(POS);
            return chunkObject.UpdateBlock(pos, isFront, blockData);
        }

        public static bool CanPlaceBlock(Vector2Int POS, SingleBlockData item, bool front)
        {
            BlockServer frontBlock = GetBlock(POS, true);
            BlockServer backBlock = GetBlock(POS, false);

            if (frontBlock != null && backBlock != null)
            {
                SingleBlockData block = (front ? frontBlock : backBlock).BlockData;
                SingleBlockData frontblock = frontBlock.BlockData;

                bool can_replace = BlockFactory.GetSlaveInstance<IReplaceable>(block.ID)?.STATIC_IsReplaceable(block, front) == true;
                bool can_place = BlockFactory.GetSlaveInstance<IPlaceable>(item.ID)?.STATIC_IsPlaceable(item, front) == true;
                bool solid_front = BlockFactory.HasInterface<ISolid>(frontblock.ID);

                return can_replace && can_place && (front || !solid_front);
            }
            else return false;
        }

        public static bool CanBreakBlock(Vector2Int POS, SingleBlockData item, SingleBlockData tool, bool front)
        {
            BlockServer frontBlock = GetBlock(POS, true);
            BlockServer backBlock = GetBlock(POS, false);

            if (frontBlock != null && backBlock != null)
            {
                SingleBlockData block = (front ? frontBlock : backBlock).BlockData;
                SingleBlockData frontblock = frontBlock.BlockData;

                if (block.ID != item.ID || block.Variant != item.Variant)
                    return false;

                bool is_breakable = BlockFactory.GetSlaveInstance<IBreakable>(block.ID)?.STATIC_IsBreakable(block, front) == true;
                bool can_mine = BlockFactory.GetSlaveInstance<IBreakable>(block.ID)?.STATIC_CanMineWith(tool) == true;
                bool solid_front = BlockFactory.HasInterface<ISolid>(frontblock.ID);

                return is_breakable && can_mine && (front || !solid_front);
            }
            else return false;
        }

        public static void PlaceBlockWithEffects(Vector2Int POS, SingleBlockData item, bool front)
        {
            UpdateBlock(POS, front, item);
        }

        public static void BreakBlockWithEffects(Vector2Int POS, SingleBlockData tool, bool front)
        {
            UpdateBlock(POS, front, new SingleBlockData { });
        }
    }
}
