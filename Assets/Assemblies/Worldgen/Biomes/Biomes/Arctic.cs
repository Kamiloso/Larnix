using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Worldgen.Ores;
using System.Collections.Generic;

namespace Larnix.Worldgen.Biomes.All
{
    public sealed class Arctic : Biome, IOreNormal
    {
        IEnumerable<Ore> IHasOre.PRIVATE_OreCache { get; set; }

        public Arctic(Seed seed) : base(seed) {}

        public override BlockData2 TranslateProtoBlock(ProtoBlock protoBlock) =>
            protoBlock switch
            {
                ProtoBlock.Air => BlockData2.Empty,

                ProtoBlock.Stone => new BlockData2(
                    new(BlockID.Ice, 0),
                    new(BlockID.Ice, 0)
                ),

                ProtoBlock.Soil or ProtoBlock.SoilSurface => new BlockData2(
                    new(BlockID.Snow, 0),
                    new()
                ),

                ProtoBlock.Cave => new BlockData2(
                    new(),
                    new(BlockID.Ice, 0)
                ),

                ProtoBlock.Liquid => new BlockData2(
                    new(BlockID.Water, 0),
                    new()
                ),

                _ => BlockData2.Empty
            };
    }
}
