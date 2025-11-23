using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Core.Utils;

namespace Larnix.Blocks
{
    public interface ILiquid : IMovingBehaviour
    {
        void Init()
        {
            This.FrameEventRandom += (sender, args) => Flow();
        }

        int FLOW_PERIOD();

        /// <summary>
        /// Use [kg / m^3] for consistency
        /// </summary>
        int LIQUID_DENSITY();
        byte MEMORY_LEFT() => 0b0001;
        byte MEMORY_RIGHT() => 0b0010;

        private void Flow()
        {
            if (WorldAPI.FramesSinceServerStart() % FLOW_PERIOD() != 0)
                return;

            Vector2Int localpos = This.Position;
            Vector2Int downpos = localpos - new Vector2Int(0, 1);

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
                Vector2Int leftpos = localpos - new Vector2Int(1, 0);
                Vector2Int rightpos = localpos + new Vector2Int(1, 0);
                Vector2Int leftpos_up = leftpos + new Vector2Int(0, 1);
                Vector2Int rightpos_up = rightpos + new Vector2Int(0, 1);

                bool can_left = CanMove(localpos, leftpos, This.IsFront);
                bool can_right = CanMove(localpos, rightpos, This.IsFront);
                bool can_left_up = CanMove(leftpos, leftpos_up, This.IsFront);
                bool can_right_up = CanMove(rightpos, rightpos_up, This.IsFront);

                byte byted_left = (byte)(byted_none | MEMORY_LEFT());
                byte byted_right = (byte)(byted_none | MEMORY_RIGHT());

                if(mem_direction == 0)
                {
                    if (can_left && can_right)
                        mem_direction = Common.Rand().Next() % 2 == 0 ? -1 : 1;

                    else if (can_left)
                        mem_direction = -1;

                    else if (can_right)
                        mem_direction = 1;
                }

                // actual move

                if(mem_direction == -1)
                {
                    if (can_left)
                    {
                        if (can_left_up)
                            Move(leftpos, leftpos_up, This.IsFront);
                        Move(localpos, leftpos, This.IsFront, byted_left);
                    }
                    else
                    {
                        WorldAPI.SetBlockVariant(localpos, This.IsFront, byted_none);
                    }
                }

                if(mem_direction == 1)
                {
                    if (can_right)
                    {
                        if (can_right_up)
                            Move(rightpos, rightpos_up, This.IsFront);
                        Move(localpos, rightpos, This.IsFront, byted_right);
                    }
                    else
                    {
                        WorldAPI.SetBlockVariant(localpos, This.IsFront, byted_none);
                    }
                }
            }
        }
    }
}
