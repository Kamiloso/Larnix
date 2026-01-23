using Larnix.Worldgen;
using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;

namespace Larnix.Worldgen.Biomes
{
    public class Desert : Biome
    {
        private Desert() {}

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
                    new BlockData1 { ID = BlockID.Sandstone },
                    new BlockData1 { ID = BlockID.Sandstone }
                    );

                case ProtoBlock.Soil:
                case ProtoBlock.SoilSurface:
                    return new BlockData2(
                    new BlockData1 { ID = BlockID.Sand },
                    new BlockData1 { }
                    );

                case ProtoBlock.Cave:
                    return new BlockData2(
                    new BlockData1 { },
                    new BlockData1 { ID = BlockID.Sandstone }
                    );

                case ProtoBlock.Liquid:
                    return new BlockData2(
                    new BlockData1 { },
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
