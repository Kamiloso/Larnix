using System.Collections;
using System.Collections.Generic;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Model.Worldgen.Biomes.All;

public sealed class Empty : Biome
{
    public Empty(Seed seed) : base(seed) {}

    public override BlockHeader2 TranslateProtoBlock(ProtoBlock protoBlock)
    {
        return BlockHeader2.Empty;
    }
}
