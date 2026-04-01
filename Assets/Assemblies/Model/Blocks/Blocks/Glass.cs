using Larnix.Model.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Model.Blocks.All;

public sealed class Glass : Block, ISolid
{
    public bool ALLOW_PLACE_BACK() => true;

    ContureType IHasConture.STATIC_DefinedAlphaEnum(byte variant) => ContureType.Disabled;

    public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
    public ITool.Tier MATERIAL_TIER() => ITool.Tier.None;
}
