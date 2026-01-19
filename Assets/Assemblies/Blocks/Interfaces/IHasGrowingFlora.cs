using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;

namespace Larnix.Blocks
{
    public interface IHasGrowingFlora : IBlockInterface
    {
        void Init()
        {
            This.FrameEventSequential += (sender, args) => FloraDry();
            This.FrameEventSequential += (sender, args) => FloraGrowth();
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
                        WorldAPI.SetBlockVariant(This.Position, This.IsFront, 0);
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
                        List<BlockServer> candidates = new();

                        foreach (BlockServer neighbour in WorldAPI.GetBlocksAround(This.Position, This.IsFront))
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
                            WorldAPI.SetBlockVariant(candidates[rand].Position, This.IsFront, This.BlockData.Variant);
                        }
                    }
                }
            }
        }

        private bool? IsSuppressed()
        {
            Vec2Int localpos = This.Position;
            Vec2Int remotpos = This.Position + new Vec2Int(0, 1);

            BlockServer blockserv = WorldAPI.GetBlock(remotpos, This.IsFront);
            if (blockserv == null)
                return null;

            return blockserv is ISolid || blockserv is ILiquid;
        }
    }
}
