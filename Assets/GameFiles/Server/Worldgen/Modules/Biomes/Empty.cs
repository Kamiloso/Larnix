using Larnix.Server.Worldgen;
using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Modules.Biomes
{
    public class Empty : Biome
    {
        public override BlockData TranslateProtoBlock(ProtoBlock protoBlock)
        {
            return new BlockData(new(), new());
        }
    }
}
