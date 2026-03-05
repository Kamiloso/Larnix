using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public sealed class CrudeOil : Block, ILiquid
    {
        public int FLOW_PERIOD() => 40;
        public int LIQUID_DENSITY() => 800;
        public bool LIQUID_IS_REPLACEABLE() => false;
        public bool IS_BLOCKING_FRONT() => true;

        public bool ALLOW_PLACE_BACK() => false;
    }
}
