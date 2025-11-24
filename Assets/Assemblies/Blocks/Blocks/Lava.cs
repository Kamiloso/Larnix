using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public class Lava : BlockServer, ILiquid, IReplaceable, IPlaceable
    {
        public Lava(Vector2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public int FLOW_PERIOD() => 40;
        public int LIQUID_DENSITY() => 3000;

        public bool ALLOW_PLACE_BACK() => false;
    }
}
