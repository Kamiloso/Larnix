using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Core;
using Larnix.Core.Utils;

namespace Larnix.Blocks
{
    public interface IWorldAPI : ICmdExecutor
    {
        public enum BreakMode : byte // must be byte for serialization
        {
            Replace = 0,
            Effects = 1, // drops particles
            Weak = 2, // rearm EventFlag
        }
        
        public long ServerTick { get; }

        public bool IsChunkLoaded(Vec2Int chunk, bool atomic = false);
        public bool IsBlockLoaded(Vec2Int POS)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
            return IsChunkLoaded(chunk);
        }

        public Block GetBlock(Vec2Int POS, bool front);
        
        public Block ReplaceBlock(Vec2Int POS, bool front, BlockData1 blockTemplate,
            BreakMode breakMode = BreakMode.Replace);
        
        public Block MutateBlockVariant(Vec2Int POS, bool front, byte variant);
        
        public bool CanPlaceBlock(Vec2Int POS, bool front, BlockData1 item);
        public bool CanBreakBlock(Vec2Int POS, bool front, BlockData1 item, BlockData1 tool);
        public void PlaceBlockWithEffects(Vec2Int POS, bool front, BlockData1 item);
        public void BreakBlockWithEffects(Vec2Int POS, bool front, BlockData1 tool);

        public List<Block> GetBlocksAround(Vec2Int POS, bool front)
        {
            List<Block> result = new(8);

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    Vec2Int POS_1 = new Vec2Int(POS.x + dx, POS.y + dy);
                    Block block = GetBlock(POS_1, front);
                    if (block != null)
                    {
                        result.Add(block);
                    }
                }

            return result;
        }
    }
}
