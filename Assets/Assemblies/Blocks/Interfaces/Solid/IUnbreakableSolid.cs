using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public interface IUnbreakableSolid : ISolid
    {
        bool IPlaceable.ALLOW_PLACE_BACK() => true;
        ITool.Tier IBreakable.MATERIAL_TIER() => ITool.Tier.Ultimate;
    }
}
