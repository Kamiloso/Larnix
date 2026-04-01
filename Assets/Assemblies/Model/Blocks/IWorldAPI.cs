#nullable enable
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;
using Larnix.Model.Utils;

namespace Larnix.Model.Blocks;

public interface IWorldAPI : ICmdExecutor
{
    public enum BreakMode : byte
    {
        Replace = 0,
        Effects = 1, // drops particles
        Weak = 2, // rearm event flag
    }

    public long ServerTick { get; }

    public bool IsChunkLoaded(Vec2Int chunk, bool atomic = false);
    public bool IsBlockLoaded(Vec2Int POS) => IsChunkLoaded(BlockUtils.CoordsToChunk(POS));

    public Block? GetBlock(Vec2Int POS, bool front);
    public Block? ReplaceBlock(Vec2Int POS, bool front, BlockData1 blockTemplate, BreakMode breakMode = BreakMode.Replace);
    public Block? MutateBlockVariant(Vec2Int POS, bool front, byte variant);

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

                Vec2Int POS1 = POS + new Vec2Int(dx, dy);
                Block? block = GetBlock(POS1, front);

                if (block is not null)
                {
                    result.Add(block);
                }
            }

        return result;
    }
}
