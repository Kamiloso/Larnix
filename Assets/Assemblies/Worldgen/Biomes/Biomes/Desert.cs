using System.Collections.Generic;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Worldgen.Ores;

namespace Larnix.Worldgen.Biomes.All
{
    public sealed class Desert : Biome, IOreNormal
    {
        IEnumerable<Ore> IHasOre.PRIVATE_OreCache { get; set; }

        public Desert(Seed seed) : base(seed) {}

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
