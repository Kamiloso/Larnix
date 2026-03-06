using Larnix.Blocks;
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using Larnix.Worldgen.Ores;

namespace Larnix.Worldgen.Biomes.All
{
    public sealed class Plains : Biome, IOreNormal
    {
        IEnumerable<Ore> IHasOre.PRIVATE_OreCache { get; set; }

        public Plains(Seed seed) : base(seed) { }

        public override BlockData2 TranslateProtoBlock(ProtoBlock protoBlock) =>
            protoBlock switch
            {
                ProtoBlock.Air => BlockData2.Empty,
                
                ProtoBlock.Stone => new BlockData2(
                    new(BlockID.Stone, 0),
                    new(BlockID.Stone, 0)
                ),

                ProtoBlock.Soil => new BlockData2(
                    new(BlockID.Soil, 0),
                    new()
                ),

                ProtoBlock.SoilSurface => new BlockData2(
                    new(BlockID.Soil, 1), // grass
                    new()
                ),

                ProtoBlock.Cave => new BlockData2(
                    new(),
                    new(BlockID.Stone, 0)
                ),

                ProtoBlock.Liquid => new BlockData2(
                    new(BlockID.Water, 0),
                    new()
                ),

                _ => BlockData2.Empty
            };
    }
}
