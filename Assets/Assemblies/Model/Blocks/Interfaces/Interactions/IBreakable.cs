#nullable enable
using Larnix.Model.Blocks.Structs;

namespace Larnix.Model.Blocks.All;

public interface IBreakable : IBlockInterface
{
    public enum DropMatchMode { OnlyID, IDAndVariant }

    public ITool.Type MATERIAL_TYPE();
    public ITool.Tier MATERIAL_TIER();
    public DropMatchMode DEFAULT_DROP_MATCH_MODE() => DropMatchMode.OnlyID;
    public bool HAS_BREAK_PARTICLES() => true;

    public bool STATIC_IsBreakableItemMatch(BlockHeader1 block, BlockHeader1 item)
    {
        switch (DEFAULT_DROP_MATCH_MODE())
        {
            case DropMatchMode.OnlyID:
                return block.Id == item.Id;

            case DropMatchMode.IDAndVariant:
                return block.Id == item.Id && block.Variant == item.Variant;
        }
        return false;
    }

    public bool STATIC_IsBreakable(BlockHeader1 block, BlockHeader1 tool, bool front)
    {
        if (!BlockFactory.HasInterface<ITool>(tool.Id))
            return false;

        ITool.Type tool_type = BlockFactory.GetSlaveInstance<ITool>(tool.Id)?.TOOL_TYPE() ?? ITool.Type.Normal;
        ITool.Tier tool_tier = BlockFactory.GetSlaveInstance<ITool>(tool.Id)?.TOOL_TIER() ?? ITool.Tier.None;

        bool type_ok = tool_type == MATERIAL_TYPE() || tool_type == ITool.Type.Ultimate;
        bool tier_ok = tool_tier >= MATERIAL_TIER() || tool_tier == ITool.Tier.Ultimate;

        return type_ok && tier_ok;
    }
}
