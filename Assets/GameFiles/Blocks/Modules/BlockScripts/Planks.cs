using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Server.Terrain;

namespace Larnix.Modules.Blocks
{
    public class Planks : BlockServer, ISolid, IPlaceable, IBreakable
    {
        public Planks(Vector2Int POS, SingleBlockData block, bool isFront) : base(POS, block, isFront) { }

        public bool ALLOW_PLACE_BACK() => false;

        public ITool.Type MATERIAL_TYPE() => ITool.Type.Wood;
        public ITool.Tier MATERIAL_TIER() => ITool.Tier.None;
    }
}
