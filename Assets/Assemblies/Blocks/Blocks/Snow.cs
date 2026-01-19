using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public class Snow : BlockServer, ISolid, IPlaceable, IBreakable, IFalling
    {
        public Snow(Vec2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public bool ALLOW_PLACE_BACK() => false;

        public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
        public ITool.Tier MATERIAL_TIER() => ITool.Tier.None;

        public int FALL_PERIOD() => 5;
    }
}
