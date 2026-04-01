using Larnix.Core.Vectors;

namespace Larnix.Worldgen.Biomes.All;

public interface ISkyColor : IBiomeInterface
{
    public static readonly Col32 Cold = new(200, 240, 255, 0);
    public static readonly Col32 Temperate = new(135, 206, 235, 0);
    public static readonly Col32 Hot = new(80, 180, 250, 0);
    public static readonly Col32 Night = new(10, 10, 30, 0);

    Col32 SKY_COLOR();
    Col32 NIGHT_SKY_COLOR();
}
