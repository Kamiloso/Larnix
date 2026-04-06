using Larnix.Model.Blocks;
using Larnix.Model.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Model.Worldgen.Ores;
using System.Collections.Generic;

namespace Larnix.Model.Worldgen.Biomes.All;

public sealed class Arctic : Biome, IOreNormal, ISkyColor
{
    IEnumerable<Ore> IHasOre.PRIVATE_OreCache { get; set; }

    public Arctic(Seed seed) : base(seed) {}

    Col32 ISkyColor.SKY_COLOR() => ISkyColor.Cold;
    Col32 ISkyColor.NIGHT_SKY_COLOR() => ISkyColor.Night;

    internal override BlockHeader2 TranslateProtoBlock(ProtoBlock protoBlock) =>
        protoBlock switch
        {
            ProtoBlock.Sky => BlockHeader2.Empty,

            ProtoBlock.Stone => new BlockHeader2(
                new(BlockID.Ice),
                new(BlockID.Ice)
            ),

            ProtoBlock.Dirt or ProtoBlock.Surface => new BlockHeader2(
                new(BlockID.Snow),
                new()
            ),

            ProtoBlock.Cave => new BlockHeader2(
                new(),
                new(BlockID.Ice)
            ),

            ProtoBlock.Lake => new BlockHeader2(
                new(BlockID.Water),
                new()
            ),

            _ => BlockHeader2.Empty
        };
}
