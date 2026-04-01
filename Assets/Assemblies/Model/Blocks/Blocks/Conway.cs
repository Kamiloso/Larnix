using Larnix.Model.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Model.Blocks.All;

public sealed class Conway : Block, ISolid, IConway
{
    public bool ALLOW_PLACE_BACK() => true;

    public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
    public ITool.Tier MATERIAL_TIER() => ITool.Tier.Wood;

    public int CONWAY_PERIOD() => 40;
}
