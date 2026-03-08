using System.Collections.Generic;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Worldgen.Ores;
using Larnix.Core.Vectors;
using Larnix.Core.Enums;

namespace Larnix.Worldgen.Biomes.All
{
    public sealed class Desert : Biome, IOreNormal, ISkyColor
    {
        IEnumerable<Ore> IHasOre.PRIVATE_OreCache { get; set; }

        public Desert(Seed seed) : base(seed) {}

        Col32 ISkyColor.SKY_COLOR() => ISkyColor.Hot;
        Col32 ISkyColor.NIGHT_SKY_COLOR() => ISkyColor.Night;

        public override BlockData2 TranslateProtoBlock(ProtoBlock protoBlock) =>
            protoBlock switch
            {
                ProtoBlock.Sky => BlockData2.Empty,

                ProtoBlock.Stone => new BlockData2(
                    new(BlockID.Sandstone, 0),
                    new(BlockID.Sandstone, 0)
                ),

                ProtoBlock.Dirt or ProtoBlock.Surface => new BlockData2(
                    new(BlockID.Sand, 0),
                    new()
                ),

                ProtoBlock.Cave => new BlockData2(
                    new(),
                    new(BlockID.Sandstone, 0)
                ),

                ProtoBlock.Lake => BlockData2.Empty,

                _ => BlockData2.Empty
            };
    }
}
