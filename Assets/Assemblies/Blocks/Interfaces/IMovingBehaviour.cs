using Larnix.Blocks;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public interface IMovingBehaviour : IBlockInterface
    {
        public bool CanMove(Vec2Int POS_source, Vec2Int POS_destin, bool isFront)
        {
            BlockServer block1 = WorldAPI.GetBlock(POS_source, isFront);
            BlockServer block2 = WorldAPI.GetBlock(POS_destin, isFront);

            int weight1 = (block1 as ILiquid)?.LIQUID_DENSITY() ?? (block1 is Air || block1 is IFragile ? int.MinValue : int.MaxValue);
            int weight2 = (block2 as ILiquid)?.LIQUID_DENSITY() ?? (block2 is Air || block2 is IFragile ? int.MinValue : int.MaxValue);

            return weight1 > weight2;
        }

        public void Move(Vec2Int POS_source, Vec2Int POS_destin, bool isFront,
            byte? sourceNewVariant = null, bool clone = false)
        {
            BlockServer block1 = WorldAPI.GetBlock(POS_source, isFront);
            BlockServer block2 = WorldAPI.GetBlock(POS_destin, isFront);

            BlockData1 data1 = block1.BlockData;
            BlockData1 data2 = block2.BlockData;

            if (sourceNewVariant != null) // optional variant change
            {
                data1 = new BlockData1(
                    id: data1.ID,
                    variant: sourceNewVariant.Value,
                    data: data1.Data);
            }

            if (block2 is Air || block2 is ILiquid) // swap
            {
                if (!clone) WorldAPI.ReplaceBlock(POS_source, isFront, data2);
                WorldAPI.ReplaceBlock(POS_destin, isFront, data1);
            }

            else if (block2 is IFragile) // break
            {
                WorldAPI.BreakBlockWithEffects(POS_destin, isFront, BlockData1.UltimateTool);
                if (!clone) WorldAPI.ReplaceBlock(POS_source, isFront, BlockData1.Air);
                WorldAPI.ReplaceBlock(POS_destin, isFront, data1);
            }
        }
    }
}
