using Larnix.Blocks;
using Larnix.Client;
using Larnix.Server.Terrain;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Larnix.Modules.Blocks
{
    public interface IFalling : IMovingBehaviour
    {
        void Init()
        {
            var Block = (BlockServer)this;
            // ---------------------------- //

            Block.FrameEventRandom += (sender, args) => Fall();
        }

        int FALL_PERIOD();

        private void Fall()
        {
            var Block = (BlockServer)this;
            // ---------------------------- //

            if (Server.References.Server.GetFixedFrame() % FALL_PERIOD() != 0)
                return;

            Vector2Int localpos = Block.Position;
            Vector2Int downpos = localpos - new Vector2Int(0, 1);

            if (CanMove(localpos, downpos, Block.IsFront))
            {
                Move(localpos, downpos, Block.IsFront);
            }
        }
    }
}
