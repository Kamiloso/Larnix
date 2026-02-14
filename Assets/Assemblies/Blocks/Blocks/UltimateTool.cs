using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public sealed class UltimateTool : BlockServer, ITool
    {
        public UltimateTool(Vec2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public ITool.Type TOOL_TYPE() => ITool.Type.Ultimate;
        public ITool.Tier TOOL_TIER() => ITool.Tier.Ultimate;
        public int TOOL_MAX_DURABILITY() => -1;
        public double TOOL_SPEED() => 1.0;
    }
}
