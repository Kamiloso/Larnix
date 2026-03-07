using Larnix.Worldgen.Ores;
using System.Collections.Generic;
using Larnix.Blocks;
using Larnix.Blocks.Structs;

namespace Larnix.Worldgen.Biomes.All
{
    public interface IOreNormal : IHasOre
    {
        IEnumerable<Ore> IHasOre.PRIVATE_CreateOres()
        {
            yield return new PerlinOre(This.Seed, "PLAINS: plastic_0")
            {
                BlockTransform = stone => MatchingOre(
                    stone, baseOre: new(BlockID.Plastic, 0)),
            };

            yield return new PerlinOre(This.Seed, "PLAINS: plastic_4")
            {
                BlockTransform = stone => MatchingOre(
                    stone, baseOre: new(BlockID.Plastic, 4)),
                
                MinHeight = -200,
                TransitionWidth = 250,
                MinNoise = 0.75
            };

            yield return new PerlinOre(This.Seed, "PLAINS: plastic_5")
            {
                BlockTransform = stone => MatchingOre(
                    stone, baseOre: new(BlockID.Plastic, 5)),
                
                MaxHeight = -150,
                MinNoise = 0.75
            };
        }
    }
}
