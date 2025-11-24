using Larnix.Blocks;
using Larnix.Core.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks.Structs;

namespace Larnix.Server.Terrain
{
    internal class WorldAPI : IWorldAPI
    {
        private ChunkLoading Chunks => Ref.ChunkLoading;
        public uint FramesSinceServerStart() => Ref.Server.FixedFrame;

        public BlockServer GetBlock(Vector2Int POS, bool isFront)
        {
            Vector2Int chunk = BlockUtils.CoordsToChunk(POS);
            if (Chunks.TryGetChunk(chunk, out var chunkObject))
            {
                Vector2Int pos = BlockUtils.LocalBlockCoords(POS);
                return chunkObject.GetBlock(pos, isFront);
            }
            return null;
        }

        public BlockServer ReplaceBlock(Vector2Int POS, bool isFront, BlockData1 blockData)
        {
            Vector2Int chunk = BlockUtils.CoordsToChunk(POS);
            if (Chunks.TryGetChunk(chunk, out var chunkObject))
            {
                Vector2Int pos = BlockUtils.LocalBlockCoords(POS);
                return chunkObject.UpdateBlock(pos, isFront, blockData);
            }
            return null;

        }

        public BlockServer SetBlockVariant(Vector2Int POS, bool isFront, byte Variant)
        {
            BlockServer oldBlock = GetBlock(POS, isFront);
            if (oldBlock != null)
            {
                BlockData1 data = oldBlock.BlockData.DeepCopy();
                data.Variant = Variant;
                return ReplaceBlock(POS, isFront, data);
            }
            return null;
        }

        public BlockServer SetBlockNBT(Vector2Int POS, bool isFront, string NBT)
        {
            BlockServer oldBlock = GetBlock(POS, isFront);
            if (oldBlock != null)
            {
                BlockData1 data = oldBlock.BlockData.DeepCopy();
                data.NBT = NBT;
                return ReplaceBlock(POS, isFront, data);
            }
            return null;
        }

        public bool CanPlaceBlock(Vector2Int POS, bool front, BlockData1 item)
        {
            BlockServer frontBlock = GetBlock(POS, true);
            BlockServer backBlock = GetBlock(POS, false);

            if (frontBlock != null && backBlock != null)
            {
                BlockData1 block = (front ? frontBlock : backBlock).BlockData;
                BlockData1 frontblock = frontBlock.BlockData;

                bool can_replace = BlockFactory.GetSlaveInstance<IReplaceable>(block.ID)?.STATIC_IsReplaceable(block, front) == true;
                bool can_place = BlockFactory.GetSlaveInstance<IPlaceable>(item.ID)?.STATIC_IsPlaceable(item, front) == true;
                bool solid_front = BlockFactory.HasInterface<ISolid>(frontblock.ID);

                return can_replace && can_place && (front || !solid_front);
            }
            else return false;
        }

        public bool CanBreakBlock(Vector2Int POS, bool front, BlockData1 item, BlockData1 tool)
        {
            BlockServer frontBlock = GetBlock(POS, true);
            BlockServer backBlock = GetBlock(POS, false);

            if (frontBlock != null && backBlock != null)
            {
                BlockData1 block = (front ? frontBlock : backBlock).BlockData;
                BlockData1 frontblock = frontBlock.BlockData;

                if (block.ID != item.ID || block.Variant != item.Variant)
                    return false;

                bool is_breakable = BlockFactory.GetSlaveInstance<IBreakable>(block.ID)?.STATIC_IsBreakable(block, front) == true;
                bool can_mine = BlockFactory.GetSlaveInstance<IBreakable>(block.ID)?.STATIC_CanMineWith(tool) == true;
                bool solid_front = BlockFactory.HasInterface<ISolid>(frontblock.ID);

                return is_breakable && can_mine && (front || !solid_front);
            }
            else return false;
        }

        public void PlaceBlockWithEffects(Vector2Int POS, bool front, BlockData1 item)
        {
            ReplaceBlock(POS, front, item);
        }

        public void BreakBlockWithEffects(Vector2Int POS, bool front, BlockData1 tool)
        {
            ReplaceBlock(POS, front, new BlockData1 { });
        }
    }
}
