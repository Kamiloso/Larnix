using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Worldgen.Biomes.All;

namespace Larnix.Worldgen.Biomes
{
    public abstract class Biome
    {
        public Seed Seed { get; private init; }
        
        public Col32 SkyColor => (this as ISkyColor)?.SKY_COLOR() ?? ISkyColor.Temperate;
        public Col32 NightSkyColor => (this as ISkyColor)?.NIGHT_SKY_COLOR() ?? ISkyColor.Night;

        public abstract BlockData2 TranslateProtoBlock(ProtoBlock protoBlock);

        protected Biome(Seed seed)
        {
            Seed = seed;
        }
    }
}
