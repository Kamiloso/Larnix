using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Server.Terrain;

namespace Larnix.Modules.Blocks
{
    public class WoodenPickaxe : BlockServer, ITool
    {
        public WoodenPickaxe(Vector2Int POS, SingleBlockData block, bool isFront) : base(POS, block, isFront) { }

        public ITool.Type TOOL_TYPE() => ITool.Type.Normal;
        public ITool.Tier TOOL_TIER() => ITool.Tier.Wood;
        public int TOOL_MAX_DURABILITY() => -1;
        public double TOOL_SPEED() => 1.0;
    }
}
