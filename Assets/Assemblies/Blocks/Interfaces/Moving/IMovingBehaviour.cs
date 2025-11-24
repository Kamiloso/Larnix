using Larnix.Blocks;
using UnityEngine;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public interface IMovingBehaviour : IBlockInterface
    {
        public bool CanMove(Vector2Int POS_source, Vector2Int POS_destin, bool isFront)
        {
            BlockServer block1 = WorldAPI.GetBlock(POS_source, isFront);
            BlockServer block2 = WorldAPI.GetBlock(POS_destin, isFront);

            int weight1 = (block1 as ILiquid)?.LIQUID_DENSITY() ?? (block1 is Air || block1 is IFragile ? int.MinValue : int.MaxValue);
            int weight2 = (block2 as ILiquid)?.LIQUID_DENSITY() ?? (block2 is Air || block2 is IFragile ? int.MinValue : int.MaxValue);

            return weight1 > weight2;
        }

        public void Move(Vector2Int POS_source, Vector2Int POS_destin, bool isFront, byte? sourceNewVariant = null)
        {
            BlockServer block1 = WorldAPI.GetBlock(POS_source, isFront);
            BlockServer block2 = WorldAPI.GetBlock(POS_destin, isFront);

            BlockData1 data1 = block1.BlockData;
            BlockData1 data2 = block2.BlockData;

            if(sourceNewVariant != null)
            {
                data1 = data1.DeepCopy();
                data1.Variant = sourceNewVariant ?? 0;
            }

            if (block2 is Air || block2 is ILiquid) // swap
            {
                WorldAPI.ReplaceBlock(POS_source, isFront, data2);
                WorldAPI.ReplaceBlock(POS_destin, isFront, data1);
            }

            else if(block2 is IFragile) // break
            {
                WorldAPI.BreakBlockWithEffects(POS_destin, isFront, new BlockData1 { });
                WorldAPI.ReplaceBlock(POS_source, isFront, new BlockData1 { });
                WorldAPI.ReplaceBlock(POS_destin, isFront, data1);
            }
        }
    }
}
