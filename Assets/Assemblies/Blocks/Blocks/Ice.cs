using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Blocks
{
    public class Ice : BlockServer, ISolid, IPlaceable, IBreakable
    {
        public Ice(Vector2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public bool ALLOW_PLACE_BACK() => true;

        public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
        public ITool.Tier MATERIAL_TIER() => ITool.Tier.Wood;
    }
}
