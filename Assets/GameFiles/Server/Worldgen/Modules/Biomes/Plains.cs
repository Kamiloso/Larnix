using Larnix.Server.Worldgen;
using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Modules.Biomes
{
    public class Plains : Biome
    {
        public override BlockData TranslateProtoBlock(ProtoBlock protoBlock)
        {
            switch (protoBlock)
            {
                case ProtoBlock.Air:
                    return new BlockData(
                    new SingleBlockData { },
                    new SingleBlockData { }
                    );

                case ProtoBlock.Stone:
                    return new BlockData(
                    new SingleBlockData { ID = BlockID.Stone },
                    new SingleBlockData { ID = BlockID.Stone }
                    );

                case ProtoBlock.Soil:
                    return new BlockData(
                    new SingleBlockData { ID = BlockID.Dirt },
                    new SingleBlockData { }
                    );

                case ProtoBlock.SoilSurface:
                    return new BlockData(
                    new SingleBlockData { ID = BlockID.Dirt, Variant = 1 }, // grass
                    new SingleBlockData { }
                    );

                case ProtoBlock.Cave:
                    return new BlockData(
                    new SingleBlockData { },
                    new SingleBlockData { ID = BlockID.Stone }
                    );

                case ProtoBlock.Liquid:
                    return new BlockData(
                    new SingleBlockData { ID = BlockID.Water },
                    new SingleBlockData { }
                    );

                default:
                    return new BlockData(
                    new SingleBlockData { },
                    new SingleBlockData { }
                    );
            }
        }
    }
}
