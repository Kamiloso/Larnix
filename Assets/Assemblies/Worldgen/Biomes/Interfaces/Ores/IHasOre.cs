using Larnix.Worldgen.Ores;
using System.Collections.Generic;
using Larnix.GameCore.Structs;
using System.Linq;

namespace Larnix.Worldgen.Biomes.All;

public interface IHasOre : IBiomeInterface
{
    IEnumerable<Ore> PRIVATE_OreCache { get; set; }
    IEnumerable<Ore> PRIVATE_CreateOres();

    IEnumerable<Ore> ORES()
    {
        PRIVATE_OreCache ??= PRIVATE_CreateOres().ToList();
        return PRIVATE_OreCache;
    }

    protected static BlockHeader1 MatchingOre(BlockHeader1 stone, BlockHeader1 baseOre)
    {
        // TODO: Make ores match the stone texture
        return baseOre;
    }
}
