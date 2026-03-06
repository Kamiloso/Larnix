using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;

namespace Larnix.Worldgen.Biomes.All
{
    public sealed class Empty : Biome
    {
        public Empty(Seed seed) : base(seed) {}
        
        public override BlockData2 TranslateProtoBlock(ProtoBlock protoBlock)
        {
            return BlockData2.Empty;
        }
    }
}
