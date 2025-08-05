using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Server.Terrain;

namespace Larnix.Modules.Blocks
{
    public interface IBreakable
    {
        void Init()
        {

        }

        public ITool.Type MATERIAL_TYPE();
        public ITool.Tier MATERIAL_TIER();

        public bool STATIC_IsBreakable(SingleBlockData block, bool front)
        {
            return true;
        }

        public bool STATIC_CanMineWith(SingleBlockData tool)
        {
            if (!BlockFactory.HasInterface<ITool>(tool.ID))
                return false;

            ITool.Type tool_type = BlockFactory.GetSlaveInstance<ITool>(tool.ID).TOOL_TYPE();
            ITool.Tier tool_tier = BlockFactory.GetSlaveInstance<ITool>(tool.ID).TOOL_TIER();

            bool type_ok = tool_type == MATERIAL_TYPE();
            bool tier_ok = tool_tier >= MATERIAL_TIER();

            return type_ok && tier_ok;
        }
    }
}
