using Larnix.Blocks;
using Larnix.Server.Terrain;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace Larnix.Modules.Blocks
{
    public interface IHasGrowingFlora
    {
        void Init()
        {
            var Block = (BlockServer)this;
            // ---------------------------- //

            Block.FrameEventSequential += (sender, args) => FloraDry();
            Block.FrameEventSequential += (sender, args) => FloraGrowth();
        }

        double DRY_CHANCE();
        double GROWTH_CHANCE();

        private void FloraDry()
        {
            var Block = (BlockServer)this;
            // ---------------------------- //

            if (Block.BlockData.Variant != 0)
            {
                if (Common.Rand().NextDouble() < DRY_CHANCE())
                {
                    bool? suppressed = IsSuppressed();
                    if (suppressed == true)
                        WorldAPI.UpdateBlockVariant(Block.Position, Block.IsFront, 0);
                }
            }
        }

        private void FloraGrowth()
        {
            var Block = (BlockServer)this;
            // ---------------------------- //

            if (Block.BlockData.Variant != 0)
            {
                if (Common.Rand().NextDouble() < GROWTH_CHANCE())
                {
                    bool? self_suppressed = IsSuppressed();
                    if (self_suppressed == false)
                    {
                        List<BlockServer> candidates = new();

                        foreach (BlockServer neighbour in WorldAPI.GetBlocksAround(Block.Position, Block.IsFront))
                        {
                            IHasGrowingFlora other = neighbour as IHasGrowingFlora;
                            if (other != null && Block.BlockData.ID == neighbour.BlockData.ID && neighbour.BlockData.Variant == 0)
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
                            WorldAPI.UpdateBlockVariant(candidates[rand].Position, Block.IsFront, Block.BlockData.Variant);
                        }
                    }
                }
            }
        }

        private bool? IsSuppressed()
        {
            var Block = (BlockServer)this;
            // ---------------------------- //

            Vector2Int localpos = Block.Position;
            Vector2Int remotpos = Block.Position + new Vector2Int(0, 1);

            BlockServer blockserv = WorldAPI.GetBlock(remotpos, Block.IsFront);
            if (blockserv == null)
                return null;

            return blockserv is ISolid || blockserv is ILiquid;
        }
    }
}
