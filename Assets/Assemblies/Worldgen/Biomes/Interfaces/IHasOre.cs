using Larnix.Blocks;
using Larnix.Blocks.Structs;
using System;
using Larnix.Worldgen.Ores;
using System.Collections.ObjectModel;

namespace Larnix.Worldgen.Biomes.All
{
    public interface IHasOre
    {
        /// <summary>
        /// Do not allocate new dictionary every time! Use static readonly field.
        /// null -> use default ore block.
        /// </summary>
        ReadOnlyDictionary<OreID, BlockData1> ORES();
    }
}
