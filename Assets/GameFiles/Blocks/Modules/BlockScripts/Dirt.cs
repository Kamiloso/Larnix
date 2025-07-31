using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Server.Terrain;

namespace Larnix.Modules.Blocks
{
    public class Dirt : BlockServer, ISolid, IPlaceable, IBreakable, IHasGrowingFlora
    {
        public Dirt(Vector2Int POS, SingleBlockData block, bool isFront) : base(POS, block, isFront) { }

        public bool ALLOW_PLACE_BACK() => false;

        public ITool.Type MATERIAL_TYPE() => ITool.Type.Surface;
        public ITool.Tier MATERIAL_TIER() => ITool.Tier.None;

        public double DRY_CHANCE() => 0.001;
        public double GROWTH_CHANCE() => 0.0002;
    }
}
