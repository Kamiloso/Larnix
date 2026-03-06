using Larnix.Blocks;
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using Larnix.Worldgen.Ores;
using System.Collections.ObjectModel;

namespace Larnix.Worldgen.Biomes.All
{
    public sealed class Desert : Biome, IHasOre
    {
        private Desert() {}

        ReadOnlyDictionary<OreID, BlockData1> IHasOre.ORES() => _ores;
        private static readonly ReadOnlyDictionary<OreID, BlockData1> _ores =
            new(new Dictionary<OreID, BlockData1>()
            {
                [OreID.TestOre] = new(BlockID.Log, 0),
                [OreID.BiomeTestOre] = null
            });

        public override BlockData2 TranslateProtoBlock(ProtoBlock protoBlock) =>
            protoBlock switch
            {
                ProtoBlock.Air => BlockData2.Empty,

                ProtoBlock.Stone => new BlockData2(
                    new(BlockID.Sandstone, 0),
                    new(BlockID.Sandstone, 0)
                ),

                ProtoBlock.Soil or ProtoBlock.SoilSurface => new BlockData2(
                    new(BlockID.Sand, 0),
                    new()
                ),

                ProtoBlock.Cave => new BlockData2(
                    new(),
                    new(BlockID.Sandstone, 0)
                ),

                ProtoBlock.Liquid => BlockData2.Empty,

                _ => BlockData2.Empty
            };
    }
}
