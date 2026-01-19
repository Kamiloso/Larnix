using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public interface IWorldAPI
    {
        public uint FramesSinceServerStart();
        public BlockServer GetBlock(Vec2Int POS, bool front);
        public BlockServer ReplaceBlock(Vec2Int POS, bool front, BlockData1 blockData);
        public BlockServer SetBlockVariant(Vec2Int POS, bool front, byte variant);
        public BlockServer SetBlockNBT(Vec2Int POS, bool front, string NBT);
        public bool CanPlaceBlock(Vec2Int POS, bool front, BlockData1 item);
        public bool CanBreakBlock(Vec2Int POS, bool front, BlockData1 item, BlockData1 tool);
        public void PlaceBlockWithEffects(Vec2Int POS, bool front, BlockData1 item);
        public void BreakBlockWithEffects(Vec2Int POS, bool front, BlockData1 tool);

        public List<BlockServer> GetBlocksAround(Vec2Int POS, bool front)
        {
            List<BlockServer> result = new(8);

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    Vec2Int _POS = new Vec2Int(POS.x + dx, POS.y + dy);
                    BlockServer block = GetBlock(_POS, front);
                    if (block != null)
                    {
                        result.Add(block);
                    }
                }

            return result;
        }
    }
}
