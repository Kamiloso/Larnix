using Larnix.Model.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Model.Blocks.All;

public sealed class Barrier : Block, IUnbreakableSolid
{
    public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;

    ContureType IHasConture.STATIC_DefinedAlphaEnum(byte variant) => ContureType.Disabled;
}
