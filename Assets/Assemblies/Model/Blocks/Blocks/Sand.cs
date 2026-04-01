using Larnix.Model.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Model.Blocks.All;

public sealed class Sand : Block, ISolid, IFalling
{
    public bool ALLOW_PLACE_BACK() => false;

    public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
    public ITool.Tier MATERIAL_TIER() => ITool.Tier.None;

    public int FALL_PERIOD() => 5;
}
