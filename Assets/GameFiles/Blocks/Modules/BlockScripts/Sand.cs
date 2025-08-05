using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Server.Terrain;

namespace Larnix.Modules.Blocks
{
    public class Sand : BlockServer, ISolid, IPlaceable, IBreakable, IFalling
    {
        public Sand(Vector2Int POS, SingleBlockData block, bool isFront) : base(POS, block, isFront) { }

        public bool ALLOW_PLACE_BACK() => false;

        public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
        public ITool.Tier MATERIAL_TIER() => ITool.Tier.None;

        public int FALL_PERIOD() => 5;
    }
}
