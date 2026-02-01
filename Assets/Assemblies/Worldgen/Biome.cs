using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using Larnix.Blocks.Structs;

namespace Larnix.Worldgen
{
    public abstract class Biome
    {
        public abstract BlockData2 TranslateProtoBlock(ProtoBlock protoBlock);
    }
}
