using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public sealed class Lava : BlockServer, ILiquid, IPlaceable
    {
        public Lava(Vec2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public int FLOW_PERIOD() => 40;
        public int LIQUID_DENSITY() => 3000;
        public bool LIQUID_IS_REPLACEABLE() => true;

        public bool ALLOW_PLACE_BACK() => false;
    }
}
