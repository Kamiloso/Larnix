using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public sealed class Barrier : Block, IUnbreakableSolid
    {
        public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;

        ContureType IHasConture.STATIC_DefinedAlphaEnum(byte variant) => ContureType.Disabled;
    }
}
