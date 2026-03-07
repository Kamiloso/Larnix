using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Worldgen.Ores;
using System.Collections.Generic;

namespace Larnix.Worldgen.Biomes.All
{
    public sealed class Arctic : Biome, IOreNormal, ISkyColor
    {
        IEnumerable<Ore> IHasOre.PRIVATE_OreCache { get; set; }

        public Arctic(Seed seed) : base(seed) {}

        Col32 ISkyColor.SKY_COLOR() => ISkyColor.Cold;
        Col32 ISkyColor.NIGHT_SKY_COLOR() => ISkyColor.Night;

        public override BlockData2 TranslateProtoBlock(ProtoBlock protoBlock) =>
            protoBlock switch
            {
                ProtoBlock.Sky => BlockData2.Empty,

                ProtoBlock.Stone => new BlockData2(
                    new(BlockID.Ice, 0),
                    new(BlockID.Ice, 0)
                ),

                ProtoBlock.Dirt or ProtoBlock.Surface => new BlockData2(
                    new(BlockID.Snow, 0),
                    new()
                ),

                ProtoBlock.Cave => new BlockData2(
                    new(),
                    new(BlockID.Ice, 0)
                ),

                ProtoBlock.Lake => new BlockData2(
                    new(BlockID.Water, 0),
                    new()
                ),

                _ => BlockData2.Empty
            };
    }
}
