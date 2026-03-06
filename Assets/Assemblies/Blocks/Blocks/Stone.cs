using Larnix.Blocks;

namespace Larnix.Blocks.All
{
    public sealed class Stone : Block, ISolid, IOreReplaceable
    {
        public bool ALLOW_PLACE_BACK() => true;

        public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
        public ITool.Tier MATERIAL_TIER() => ITool.Tier.Wood;
    }
}
