using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public sealed class Bedrock : Block, IUnbreakableSolid
    {
        public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
    }
}
