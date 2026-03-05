using Larnix.Blocks;
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using Larnix.Worldgen.Biomes.Interfaces;
using Larnix.Worldgen.Ores;
using System;

namespace Larnix.Worldgen.Biomes.All
{
    public sealed class Desert : Biome, IHasOre
    {
        Dictionary<OreID, BlockData1> IHasOre.ORES() => new() {
            { OreID.TestOre, new(BlockID.Log,0)},
            { OreID.BiomeTestOre, new(BlockID.Planks,0) }
        };
        Type IHasOre.BIOME() => typeof(Desert);

        private Desert() {}

        public override BlockData2 TranslateProtoBlock(ProtoBlock protoBlock)
        {
            switch (protoBlock)
            {
                case ProtoBlock.Air:
                    return new BlockData2();

                case ProtoBlock.Stone:
                    return new BlockData2(
                        new(BlockID.Sandstone, 0),
                        new(BlockID.Sandstone, 0)
                    );

                case ProtoBlock.Soil:
                case ProtoBlock.SoilSurface:
                    return new BlockData2(
                        new(BlockID.Sand, 0),
                        new()
                    );

                case ProtoBlock.Cave:
                    return new BlockData2(
                        new(),
                        new(BlockID.Sandstone, 0)
                    );

                case ProtoBlock.Liquid:
                    return new BlockData2();

                default:
                    return new BlockData2();
            }
        }
    }
}
