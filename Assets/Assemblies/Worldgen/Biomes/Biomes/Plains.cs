using Larnix.Blocks;
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using Larnix.Worldgen.Ores;
using Larnix.Core.Vectors;
using Larnix.Core.Enums;

namespace Larnix.Worldgen.Biomes.All
{
    public sealed class Plains : Biome, IOreNormal, ISkyColor
    {
        IEnumerable<Ore> IHasOre.PRIVATE_OreCache { get; set; }

        public Plains(Seed seed) : base(seed) { }

        Col32 ISkyColor.SKY_COLOR() => ISkyColor.Temperate;
        Col32 ISkyColor.NIGHT_SKY_COLOR() => ISkyColor.Night;

        public override BlockData2 TranslateProtoBlock(ProtoBlock protoBlock) =>
            protoBlock switch
            {
                ProtoBlock.Sky => BlockData2.Empty,
                
                ProtoBlock.Stone => new BlockData2(
                    new(BlockID.Stone, 0),
                    new(BlockID.Stone, 0)
                ),

                ProtoBlock.Dirt => new BlockData2(
                    new(BlockID.Soil, 0),
                    new()
                ),

                ProtoBlock.Surface => new BlockData2(
                    new(BlockID.Soil, 1), // grass
                    new()
                ),

                ProtoBlock.Cave => new BlockData2(
                    new(),
                    new(BlockID.Stone, 0)
                ),

                ProtoBlock.Lake => new BlockData2(
                    new(BlockID.Water, 0),
                    new()
                ),

                _ => BlockData2.Empty
            };
    }
}
