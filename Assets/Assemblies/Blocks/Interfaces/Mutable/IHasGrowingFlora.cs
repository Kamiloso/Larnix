using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;

namespace Larnix.Blocks.All
{
    public interface IHasGrowingFlora : IBlockInterface
    {
        void Init()
        {
            This.Subscribe(BlockOrder.Random, () => {
                FloraDry();
                FloraGrowth();
            });
        }

        double DRY_CHANCE();
        double GROWTH_CHANCE();

        private void FloraDry()
        {
            if (This.BlockData.Variant != 0)
            {
                if (Common.Rand().NextDouble() < DRY_CHANCE())
                {
                    bool? suppressed = IsSuppressed();
                    if (suppressed == true)
                        WorldAPI.MutateBlockVariant(This.Position, This.IsFront, 0);
                }
            }
        }

        private void FloraGrowth()
        {
            if (This.BlockData.Variant != 0)
            {
                if (Common.Rand().NextDouble() < GROWTH_CHANCE())
                {
                    bool? self_suppressed = IsSuppressed();
                    if (self_suppressed == false)
                    {
                        List<Block> candidates = new();

                        foreach (Block neighbour in WorldAPI.GetBlocksAround(This.Position, This.IsFront))
                        {
                            IHasGrowingFlora other = neighbour as IHasGrowingFlora;
                            if (other != null && This.BlockData.ID == neighbour.BlockData.ID && neighbour.BlockData.Variant == 0)
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
                            WorldAPI.MutateBlockVariant(candidates[rand].Position, This.IsFront, This.BlockData.Variant);
                        }
                    }
                }
            }
        }

        private bool? IsSuppressed()
        {
            Vec2Int POS = This.Position;
            Vec2Int POS_other = POS + new Vec2Int(0, 1);

            Block block = WorldAPI.GetBlock(POS_other, This.IsFront);
            if (block == null)
                return null;

            return block is ISolid || block is ILiquid;
        }
    }
}
