using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Core;

namespace Larnix.Blocks
{
    public interface IHasGrowingFlora : IBlockInterface
    {
        void Init()
        {
            ThisBlock.FrameEventSequential += (sender, args) => FloraDry();
            ThisBlock.FrameEventSequential += (sender, args) => FloraGrowth();
        }

        double DRY_CHANCE();
        double GROWTH_CHANCE();

        private void FloraDry()
        {
            if (ThisBlock.BlockData.Variant != 0)
            {
                if (Common.Rand().NextDouble() < DRY_CHANCE())
                {
                    bool? suppressed = IsSuppressed();
                    if (suppressed == true)
                        WorldAPI.SetBlockVariant(ThisBlock.Position, ThisBlock.IsFront, 0);
                }
            }
        }

        private void FloraGrowth()
        {
            if (ThisBlock.BlockData.Variant != 0)
            {
                if (Common.Rand().NextDouble() < GROWTH_CHANCE())
                {
                    bool? self_suppressed = IsSuppressed();
                    if (self_suppressed == false)
                    {
                        List<BlockServer> candidates = new();

                        foreach (BlockServer neighbour in WorldAPI.GetBlocksAround(ThisBlock.Position, ThisBlock.IsFront))
                        {
                            IHasGrowingFlora other = neighbour as IHasGrowingFlora;
                            if (other != null && ThisBlock.BlockData.ID == neighbour.BlockData.ID && neighbour.BlockData.Variant == 0)
                            {
                                bool? other_suppressed = other.IsSuppressed();
                                if (other_suppressed == false)
                                {
                                    candidates.Add(neighbour);
                                }
                            }
                        }

                        if(candidates.Count > 0)
                        {
                            int rand = Common.Rand().Next(0, candidates.Count);
                            WorldAPI.SetBlockVariant(candidates[rand].Position, ThisBlock.IsFront, ThisBlock.BlockData.Variant);
                        }
                    }
                }
            }
        }

        private bool? IsSuppressed()
        {
            Vector2Int localpos = ThisBlock.Position;
            Vector2Int remotpos = ThisBlock.Position + new Vector2Int(0, 1);

            BlockServer blockserv = WorldAPI.GetBlock(remotpos, ThisBlock.IsFront);
            if (blockserv == null)
                return null;

            return blockserv is ISolid || blockserv is ILiquid;
        }
    }
}
