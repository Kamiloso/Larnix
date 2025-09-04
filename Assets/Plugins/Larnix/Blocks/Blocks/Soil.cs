using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Blocks
{
    public class Soil : BlockServer, ISolid, IPlaceable, IBreakable, IHasGrowingFlora
    {
        public Soil(Vector2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public bool ALLOW_PLACE_BACK() => true;

        public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
        public ITool.Tier MATERIAL_TIER() => ITool.Tier.None;

        public double DRY_CHANCE() => 0.001;
        public double GROWTH_CHANCE() => 0.0002;
    }
}
