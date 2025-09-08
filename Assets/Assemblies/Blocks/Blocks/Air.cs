using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Blocks
{
    public class Air : BlockServer, IReplaceable, ITool
    {
        public Air(Vector2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public ITool.Type TOOL_TYPE() => ITool.Type.Normal;
        public ITool.Tier TOOL_TIER() => ITool.Tier.None;
        public int TOOL_MAX_DURABILITY() => -1;
        public double TOOL_SPEED() => 1.0;
    }
}
