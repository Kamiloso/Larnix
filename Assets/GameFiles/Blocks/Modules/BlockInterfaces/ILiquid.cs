using Larnix.Blocks;
using Larnix.Client;
using Larnix.Server.Terrain;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Larnix.Modules.Blocks
{
    public interface ILiquid
    {
        void Init()
        {
            var Block = (BlockServer)this;
            // ---------------------------- //

            Block.FrameEvent += (sender, args) => Flow();
        }

        int FLOW_PERIOD();

        private void Flow()
        {
            var Block = (BlockServer)this;
            // ---------------------------- //

            if (Server.References.Server.GetFixedFrame() % FLOW_PERIOD() != 0)
                return;

            Vector2Int localpos = Block.Position;
            Vector2Int downpos = localpos - new Vector2Int(0, 1);

            bool can_down = WillBlockBreak(downpos, Block.IsFront) == true;

            if(can_down == true) // go down
            {
                MoveInto(downpos, Block.IsFront);
            }
            else // go to side
            {
                Vector2Int rightpos = localpos + new Vector2Int(1, 0);
                Vector2Int leftpos = localpos - new Vector2Int(1, 0);

                bool can_right = WillBlockBreak(rightpos, Block.IsFront) == true;
                bool can_left = WillBlockBreak(leftpos, Block.IsFront) == true;

                if (!can_right && !can_left)
                    return;

                else if(can_left && can_right)
                {
                    bool random = Common.Rand().Next() % 2 == 0;

                    if(random) MoveInto(rightpos, Block.IsFront);
                    else MoveInto(leftpos, Block.IsFront);
                }

                else
                {
                    if (can_right) MoveInto(rightpos, Block.IsFront);
                    else MoveInto(leftpos, Block.IsFront);
                }
            }
        }

        private void MoveInto(Vector2Int POS, bool isFront)
        {
            var Block = (BlockServer)this;
            // ---------------------------- //

            SingleBlockData data = Block.BlockData;
            WorldAPI.UpdateBlock(Block.Position, Block.IsFront, new SingleBlockData { });
            WorldAPI.UpdateBlock(POS, isFront, data);
        }

        private static bool? WillBlockBreak(Vector2Int POS, bool isFront)
        {
            BlockServer block = WorldAPI.GetBlock(POS, isFront);
            if (block == null)
                return null;

            return block is Air || block is IBreaksOnLiquidContact;
        }
    }
}
