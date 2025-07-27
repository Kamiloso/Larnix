using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Server.Terrain;
using UnityEngine.Scripting;

namespace Larnix.Modules.Blocks
{
    public class Stone : BlockServer
    {
        public Stone(Vector2Int POS, SingleBlockData block, bool isFront) : base(POS, block, isFront) { }

        protected override void FrameUpdate()
        {
            // Falling stone testing system

            /*if (UnityEngine.Random.Range(0, 200) != 0)
                return;

            Vector2Int pos_here = Position;
            Vector2Int pos_there = pos_here - new Vector2Int(0, 1);
            BlockServer blockDown = GetBlock(pos_there, true);

            if (blockDown?.BlockData.ID == BlockID.Air)
            {
                SetBlock(pos_here, true, new SingleBlockData { ID = BlockID.Air });
                SetBlock(pos_there, true, new SingleBlockData { ID = BlockID.Stone });
            }*/
        }
    }
}
