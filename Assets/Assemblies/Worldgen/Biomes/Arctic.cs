using Larnix.Worldgen;
using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;

namespace Larnix.Worldgen.Biomes
{
    public sealed class Arctic : Biome
    {
        private Arctic() {}

        public override BlockData2 TranslateProtoBlock(ProtoBlock protoBlock)
        {
            switch (protoBlock)
            {
                case ProtoBlock.Air:
                    return new BlockData2();

                case ProtoBlock.Stone:
                    return new BlockData2(
                        new(BlockID.Ice, 0),
                        new(BlockID.Ice, 0)
                    );

                case ProtoBlock.Soil:
                case ProtoBlock.SoilSurface:
                    return new BlockData2(
                        new(BlockID.Snow, 0),
                        new()
                    );

                case ProtoBlock.Cave:
                    return new BlockData2(
                        new(),
                        new(BlockID.Ice, 0)
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
