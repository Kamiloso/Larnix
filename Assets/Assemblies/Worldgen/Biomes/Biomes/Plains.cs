using Larnix.Worldgen;
using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;

namespace Larnix.Worldgen.Biomes.All
{
    public sealed class Plains : Biome
    {
        private Plains() {}

        public override BlockData2 TranslateProtoBlock(ProtoBlock protoBlock)
        {
            switch (protoBlock)
            {
                case ProtoBlock.Air:
                    return new BlockData2();

                case ProtoBlock.Stone:
                    return new BlockData2(
                        new(BlockID.Stone, 0),
                        new(BlockID.Stone, 0)
                    );

                case ProtoBlock.Soil:
                    return new BlockData2(
                        new(BlockID.Soil, 0),
                        new()
                    );

                case ProtoBlock.SoilSurface:
                    return new BlockData2(
                        new(BlockID.Soil, 1), // grass
                        new()
                    );

                case ProtoBlock.Cave:
                    return new BlockData2(
                        new(),
                        new(BlockID.Stone, 0)
                    );

                case ProtoBlock.Liquid:
                    return new BlockData2(
                        new(BlockID.Water, 0),
                        new()
                    );

                default:
                    return new BlockData2();
            }
        }
    }
}
