using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public sealed class Lamp : Block, ISolid, ILamp
    {
        public bool ALLOW_PLACE_BACK() => true;

        public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
        public ITool.Tier MATERIAL_TIER() => ITool.Tier.Copper;
    }
}
