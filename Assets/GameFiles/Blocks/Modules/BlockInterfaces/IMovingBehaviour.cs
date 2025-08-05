using Larnix.Blocks;
using Larnix.Client;
using Larnix.Server.Terrain;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Larnix.Modules.Blocks
{
    public interface IMovingBehaviour
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

            SingleBlockData data1 = block1.BlockData;
            SingleBlockData data2 = block2.BlockData;

            if(sourceNewVariant != null)
            {
                data1 = data1.ShallowCopy();
                data1.Variant = sourceNewVariant ?? 0;
            }

            if (block2 is Air || block2 is ILiquid) // swap
            {
                WorldAPI.UpdateBlock(POS_source, isFront, data2);
                WorldAPI.UpdateBlock(POS_destin, isFront, data1);
            }

            else if(block2 is IFragile) // break
            {
                WorldAPI.BreakBlockWithEffects(POS_destin, new SingleBlockData { }, isFront);
                WorldAPI.UpdateBlock(POS_source, isFront, new SingleBlockData { });
                WorldAPI.UpdateBlock(POS_destin, isFront, data1);
            }
        }
    }
}
