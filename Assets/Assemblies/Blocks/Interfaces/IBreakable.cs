using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public interface IBreakable : IBlockInterface
    {
        public ITool.Type MATERIAL_TYPE();
        public ITool.Tier MATERIAL_TIER();

        public bool STATIC_IsBreakable(BlockData1 block, bool front)
        {
            return true;
        }

        public bool STATIC_CanMineWith(BlockData1 tool)
        {
            if (!BlockFactory.HasInterface<ITool>(tool.ID))
                return false;

            ITool.Type tool_type = BlockFactory.GetSlaveInstance<ITool>(tool.ID).TOOL_TYPE();
            ITool.Tier tool_tier = BlockFactory.GetSlaveInstance<ITool>(tool.ID).TOOL_TIER();

            bool type_ok = tool_type == MATERIAL_TYPE() || tool_type == ITool.Type.Ultimate;
            bool tier_ok = tool_tier >= MATERIAL_TIER() || tool_tier == ITool.Tier.Ultimate;

            return type_ok && tier_ok;
        }
    }
}
