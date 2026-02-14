using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public sealed class Conway : BlockServer, ISolid, IConway
    {
        public Conway(Vec2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public bool ALLOW_PLACE_BACK() => true;

        public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
        public ITool.Tier MATERIAL_TIER() => ITool.Tier.Wood;

        public int CONWAY_PERIOD() => 40;
    }
}
