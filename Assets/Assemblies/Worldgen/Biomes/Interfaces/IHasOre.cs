
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Larnix.Worldgen.Ores;

namespace Larnix.Worldgen.Biomes.Interfaces
{
    public interface IHasOre
    {
        Dictionary<OreID, BlockData1> ORES();
        Type BIOME();

        BlockData1 STATIC_GetOreBlock(OreID oreID, BlockID baseBlockID)
        {
            var Ores = ORES();

            if (!Ores.Keys.Contains(oreID)) throw new Exception("Incorrect OreID");

            Type Biome = BIOME();

            byte interfaceIndex = (byte)(BiomeID)Enum.Parse(typeof(BiomeID), Biome.Name);

            BlockData1 outOreBlock = Ores[oreID] ?? new(baseBlockID, interfaceIndex);

            return outOreBlock;
        }
    }
}
