using Larnix.Model.Blocks;
using System.Collections.Generic;
using Larnix.Model.Blocks.Structs;
using Larnix.Model.Worldgen.Ores;
using Larnix.Core.Vectors;

namespace Larnix.Model.Worldgen.Biomes.All;

public sealed class Plains : Biome, IOreNormal, ISkyColor
{
    IEnumerable<Ore> IHasOre.PRIVATE_OreCache { get; set; }

    public Plains(Seed seed) : base(seed) { }

    Col32 ISkyColor.SKY_COLOR() => ISkyColor.Temperate;
    Col32 ISkyColor.NIGHT_SKY_COLOR() => ISkyColor.Night;

    public override BlockHeader2 TranslateProtoBlock(ProtoBlock protoBlock) =>
        protoBlock switch
        {
            ProtoBlock.Sky => BlockHeader2.Empty,

            ProtoBlock.Stone => new BlockHeader2(
                new(BlockID.Stone, 0),
                new(BlockID.Stone, 0)
            ),

            ProtoBlock.Dirt => new BlockHeader2(
                new(BlockID.Soil, 0),
                new()
            ),

            ProtoBlock.Surface => new BlockHeader2(
                new(BlockID.Soil, 1), // grass
                new()
            ),

            ProtoBlock.Cave => new BlockHeader2(
                new(),
                new(BlockID.Stone, 0)
            ),

            ProtoBlock.Lake => new BlockHeader2(
                new(BlockID.Water, 0),
                new()
            ),

            _ => BlockHeader2.Empty
        };
}
