using System.Collections.Generic;
using Larnix.Worldgen.Ores;
using Larnix.Core.Vectors;
using Larnix.Core.Enums;
using Larnix.GameCore.Structs;

namespace Larnix.Worldgen.Biomes.All
{
    public sealed class Desert : Biome, IOreNormal, ISkyColor
    {
        IEnumerable<Ore> IHasOre.PRIVATE_OreCache { get; set; }

        public Desert(Seed seed) : base(seed) {}

        Col32 ISkyColor.SKY_COLOR() => ISkyColor.Hot;
        Col32 ISkyColor.NIGHT_SKY_COLOR() => ISkyColor.Night;

        public override BlockHeader2 TranslateProtoBlock(ProtoBlock protoBlock) =>
            protoBlock switch
            {
                ProtoBlock.Sky => BlockHeader2.Empty,

                ProtoBlock.Stone => new BlockHeader2(
                    new(BlockID.Sandstone, 0),
                    new(BlockID.Sandstone, 0)
                ),

                ProtoBlock.Dirt or ProtoBlock.Surface => new BlockHeader2(
                    new(BlockID.Sand, 0),
                    new()
                ),

                ProtoBlock.Cave => new BlockHeader2(
                    new(),
                    new(BlockID.Sandstone, 0)
                ),

                ProtoBlock.Lake => BlockHeader2.Empty,

                _ => BlockHeader2.Empty
            };
    }
}
