using Larnix.Blocks;
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using Larnix.Worldgen.Ores;
using System.Collections.ObjectModel;

namespace Larnix.Worldgen.Biomes.All
{
    public sealed class Arctic : Biome, IHasOre
    {
        private Arctic() {}

        ReadOnlyDictionary<OreID, BlockData1> IHasOre.ORES() => _ores;
        private static readonly ReadOnlyDictionary<OreID, BlockData1> _ores =
            new(new Dictionary<OreID, BlockData1>()
            {
                [OreID.TestOre] = null,
            });

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
