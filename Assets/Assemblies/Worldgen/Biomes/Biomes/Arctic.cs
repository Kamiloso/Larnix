using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Core.Enums;
using Larnix.Core.Vectors;
using Larnix.GameCore.Structs;
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

        public override BlockHeader2 TranslateProtoBlock(ProtoBlock protoBlock) =>
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
}
