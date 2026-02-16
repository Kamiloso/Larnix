using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public sealed class Acid : BlockServer, ILiquid, IElectricSource
    {
        public Acid(Vec2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public int FLOW_PERIOD() => 10;
        public int LIQUID_DENSITY() => 1000;
        public bool LIQUID_IS_REPLACEABLE() => true;
        public bool IS_BLOCKING_FRONT() => false;

        public bool ALLOW_PLACE_BACK() => false;

        public byte ElectricEmissionMask() => 0b1111;
    }
}
