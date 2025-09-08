using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Blocks
{
    public interface IWorldAPI
    {
        public uint FramesSinceServerStart();
        public BlockServer GetBlock(Vector2Int POS, bool front);
        public BlockServer ReplaceBlock(Vector2Int POS, bool front, BlockData1 blockData);
        public BlockServer SetBlockVariant(Vector2Int POS, bool front, byte variant);
        public BlockServer SetBlockNBT(Vector2Int POS, bool front, string NBT);
        public bool CanPlaceBlock(Vector2Int POS, bool front, BlockData1 item);
        public bool CanBreakBlock(Vector2Int POS, bool front, BlockData1 item, BlockData1 tool);
        public void PlaceBlockWithEffects(Vector2Int POS, bool front, BlockData1 item);
        public void BreakBlockWithEffects(Vector2Int POS, bool front, BlockData1 tool);

        public List<BlockServer> GetBlocksAround(Vector2Int POS, bool front)
        {
            List<BlockServer> result = new(8);

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    Vector2Int _POS = new Vector2Int(POS.x + dx, POS.y + dy);
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
