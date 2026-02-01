using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public sealed class CrudeOil : BlockServer, ILiquid, IPlaceable
    {
        public CrudeOil(Vec2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public int FLOW_PERIOD() => 40;
        public int LIQUID_DENSITY() => 800;

        public bool ALLOW_PLACE_BACK() => false;
    }
}
