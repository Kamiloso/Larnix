using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Server.Terrain;

namespace Larnix.Modules.Blocks
{
    public class Water : BlockServer, ILiquid, IReplaceable, IPlaceable
    {
        public Water(Vector2Int POS, SingleBlockData block, bool isFront) : base(POS, block, isFront) { }

        public int FLOW_PERIOD() => 10;
        public int LIQUID_DENSITY() => 1000;

        public bool ALLOW_PLACE_BACK() => false;
    }
}
