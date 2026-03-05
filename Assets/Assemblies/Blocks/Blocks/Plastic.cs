using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using DMM = Larnix.Blocks.All.IBreakable.DropMatchMode;

namespace Larnix.Blocks.All
{
    public sealed class Plastic : Block, ISolid
    {
        public bool ALLOW_PLACE_BACK() => true;

        public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
        public ITool.Tier MATERIAL_TIER() => ITool.Tier.None;
        DMM IBreakable.DEFAULT_DROP_MATCH_MODE() => DMM.IDAndVariant;
    }
}
