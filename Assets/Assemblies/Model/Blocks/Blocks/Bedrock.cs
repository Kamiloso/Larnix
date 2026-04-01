using Larnix.Model.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Model.Blocks.All;

public sealed class Bedrock : Block, IUnbreakableSolid
{
    public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
}
