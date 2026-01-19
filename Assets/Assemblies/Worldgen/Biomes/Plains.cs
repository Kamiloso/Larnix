using Larnix.Worldgen;
using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;

namespace Larnix.Worldgen.Biomes
{
    public class Plains : Biome
    {
        public override BlockData2 TranslateProtoBlock(ProtoBlock protoBlock)
        {
            switch (protoBlock)
            {
                case ProtoBlock.Air:
                    return new BlockData2(
                    new BlockData1 { },
                    new BlockData1 { }
                    );

                case ProtoBlock.Stone:
                    return new BlockData2(
                    new BlockData1 { ID = BlockID.Stone },
                    new BlockData1 { ID = BlockID.Stone }
                    );

                case ProtoBlock.Soil:
                    return new BlockData2(
                    new BlockData1 { ID = BlockID.Soil },
                    new BlockData1 { }
                    );

                case ProtoBlock.SoilSurface:
                    return new BlockData2(
                    new BlockData1 { ID = BlockID.Soil, Variant = 1 }, // grass
                    new BlockData1 { }
                    );

                case ProtoBlock.Cave:
                    return new BlockData2(
                    new BlockData1 { },
                    new BlockData1 { ID = BlockID.Stone }
                    );

                case ProtoBlock.Liquid:
                    return new BlockData2(
                    new BlockData1 { ID = BlockID.Water },
                    new BlockData1 { }
                    );

                default:
                    return new BlockData2(
                    new BlockData1 { },
                    new BlockData1 { }
                    );
            }
        }
    }
}
