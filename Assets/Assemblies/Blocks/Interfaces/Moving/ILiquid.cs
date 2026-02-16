using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public interface ILiquid : IMovingBehaviour, IPlaceable, IReplaceable, IBlockingFront
    {
        void Init()
        {
            This.Subscribe(BlockEvent.Random,
                 () => Flow());
        }

        bool IReplaceable.STATIC_IsReplaceable(BlockData1 thisBlock, BlockData1 otherBlock, bool isFront)
        {
            if (LIQUID_IS_REPLACEABLE())
            {
                // can only be replaced by blocks with different IDs
                return thisBlock.ID != otherBlock.ID;
            }
            else
            {
                // cannot be replaced by any block
                return false;
            }
        }

        int FLOW_PERIOD();

        /// <summary>
        /// Use [kg / m^3] for consistency
        /// </summary>
        int LIQUID_DENSITY();
        byte MEMORY_LEFT() => 0b0001;
        byte MEMORY_RIGHT() => 0b0010;
        bool LIQUID_IS_REPLACEABLE();

        bool IPlaceable.HAS_PLACE_PARTICLES() => true;

        private void Flow()
        {
            if (WorldAPI.ServerTick() % FLOW_PERIOD() != 0)
                return;

            Vec2Int localpos = This.Position;
            Vec2Int downpos = localpos - new Vec2Int(0, 1);

            int mem_direction = 
                ((This.BlockData.Variant & MEMORY_RIGHT()) != 0 ? 1 : 0) -
                ((This.BlockData.Variant & MEMORY_LEFT()) != 0 ? 1 : 0);

            byte flagged_free = (byte)~(MEMORY_LEFT() | MEMORY_RIGHT());
            byte byted_none = (byte)(flagged_free & This.BlockData.Variant);

            if (CanMove(localpos, downpos, This.IsFront)) // go down
            {
                Move(localpos, downpos, This.IsFront, byted_none);
            }
            else // go to side
            {
                Vec2Int leftpos = localpos - new Vec2Int(1, 0);
                Vec2Int rightpos = localpos + new Vec2Int(1, 0);
                Vec2Int leftpos_up = leftpos + new Vec2Int(0, 1);
                Vec2Int rightpos_up = rightpos + new Vec2Int(0, 1);

                bool can_left = CanMove(localpos, leftpos, This.IsFront);
                bool can_right = CanMove(localpos, rightpos, This.IsFront);
                bool can_left_up = CanMove(leftpos, leftpos_up, This.IsFront);
                bool can_right_up = CanMove(rightpos, rightpos_up, This.IsFront);

                byte byted_left = (byte)(byted_none | MEMORY_LEFT());
                byte byted_right = (byte)(byted_none | MEMORY_RIGHT());

                if (mem_direction == 0)
                {
                    if (can_left && can_right)
                        mem_direction = Common.Rand().Next() % 2 == 0 ? -1 : 1;

                    else if (can_left)
                        mem_direction = -1;

                    else if (can_right)
                        mem_direction = 1;
                }

                // actual move

                if (mem_direction == -1)
                {
                    if (can_left)
                    {
                        if (can_left_up)
                            Move(leftpos, leftpos_up, This.IsFront);
                        Move(localpos, leftpos, This.IsFront, byted_left);
                    }
                    else
                    {
                        WorldAPI.MutateBlockVariant(localpos, This.IsFront, byted_none);
                    }
                }

                if (mem_direction == 1)
                {
                    if (can_right)
                    {
                        if (can_right_up)
                            Move(rightpos, rightpos_up, This.IsFront);
                        Move(localpos, rightpos, This.IsFront, byted_right);
                    }
                    else
                    {
                        WorldAPI.MutateBlockVariant(localpos, This.IsFront, byted_none);
                    }
                }
            }
        }
    }
}
