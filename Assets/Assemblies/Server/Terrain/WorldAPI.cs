using Larnix.Blocks;
using Larnix.Core.Utils;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Server.Terrain
{
    internal class WorldAPI : Singleton, IWorldAPI
    {
        private ChunkLoading Chunks => Ref<ChunkLoading>();
        public uint FramesSinceServerStart() => Ref<Server>().FixedFrame;

        public WorldAPI(Server server) : base(server) { }

        public BlockServer GetBlock(Vec2Int POS, bool isFront)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
            if (Chunks.TryGetChunk(chunk, out var chunkObject))
            {
                Vec2Int pos = BlockUtils.LocalBlockCoords(POS);
                return chunkObject.GetBlock(pos, isFront);
            }
            return null;
        }

        public BlockServer ReplaceBlock(Vec2Int POS, bool isFront, BlockData1 blockTemplate)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
            if (Chunks.TryGetChunk(chunk, out var chunkObject))
            {
                Vec2Int pos = BlockUtils.LocalBlockCoords(POS);
                BlockData1 blockDeepCopy = blockTemplate.DeepCopy();
                return chunkObject.UpdateBlock(pos, isFront, blockDeepCopy);
            }
            return null;
        }

        public BlockServer SetBlockVariant(Vec2Int POS, bool isFront, byte variant)
        {
            BlockServer oldBlock = GetBlock(POS, isFront);
            if (oldBlock != null)
            {
                BlockData1 blockTemplate = new BlockData1(
                    id: oldBlock.BlockData.ID,
                    variant: variant,
                    data: oldBlock.BlockData.Data);
                
                return ReplaceBlock(POS, isFront, blockTemplate);
            }
            return null;
        }

        public bool CanPlaceBlock(Vec2Int POS, bool front, BlockData1 item)
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

        public bool CanBreakBlock(Vec2Int POS, bool front, BlockData1 item, BlockData1 tool)
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

        public void PlaceBlockWithEffects(Vec2Int POS, bool front, BlockData1 item)
        {
            ReplaceBlock(POS, front, item);
        }

        public void BreakBlockWithEffects(Vec2Int POS, bool front, BlockData1 tool)
        {
            ReplaceBlock(POS, front, new BlockData1());
        }
    }
}
