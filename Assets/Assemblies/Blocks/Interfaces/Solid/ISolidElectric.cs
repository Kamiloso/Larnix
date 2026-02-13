using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public interface ISolidElectric : ISolid
    {
        bool IPlaceable.ALLOW_PLACE_BACK() => true;

        ITool.Type IBreakable.MATERIAL_TYPE() => ITool.Type.Normal;
        ITool.Tier IBreakable.MATERIAL_TIER() => ITool.Tier.Copper;
    }
}
