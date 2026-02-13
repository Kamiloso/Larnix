using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using Larnix.Core.Binary;
using System;

namespace Larnix.Blocks
{
    public interface IBreakable : IBlockInterface
    {
        public enum DropMatchMode { OnlyID, IDAndVariant }

        public ITool.Type MATERIAL_TYPE();
        public ITool.Tier MATERIAL_TIER();
        public DropMatchMode DEFAULT_DROP_MATCH_MODE() => DropMatchMode.OnlyID;
        public bool HAS_BREAK_PARTICLES() => true;

        public bool STATIC_IsBreakableItemMatch(BlockData1 block, BlockData1 item)
        {
            switch (DEFAULT_DROP_MATCH_MODE())
            {
                case DropMatchMode.OnlyID:
                    return block.ID == item.ID;
                
                case DropMatchMode.IDAndVariant:
                    return block.ID == item.ID && block.Variant == item.Variant;
            }
            return false;
        }

        public bool STATIC_IsBreakable(BlockData1 block, BlockData1 tool, bool front)
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
