using Larnix.Worldgen;
using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;

namespace Larnix.Worldgen.Biomes
{
    public class Empty : Biome
    {
        private Empty() {}
        
        public override BlockData2 TranslateProtoBlock(ProtoBlock protoBlock)
        {
            return new BlockData2(new(), new());
        }
    }
}
