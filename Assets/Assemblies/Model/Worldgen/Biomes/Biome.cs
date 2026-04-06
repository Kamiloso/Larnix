using Larnix.Model.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Model.Worldgen.Biomes.All;

namespace Larnix.Model.Worldgen.Biomes;

public abstract class Biome
{
    public Seed Seed { get; private init; }

    public Col32 SkyColor => (this as ISkyColor)?.SKY_COLOR() ?? ISkyColor.Temperate;
    public Col32 NightSkyColor => (this as ISkyColor)?.NIGHT_SKY_COLOR() ?? ISkyColor.Night;

    internal abstract BlockHeader2 TranslateProtoBlock(ProtoBlock protoBlock);

    protected Biome(Seed seed)
    {
        Seed = seed;
    }
}
