using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using Larnix.Blocks.Structs;

namespace Larnix.Worldgen
{
    public abstract class Biome
    {
        public abstract BlockData2 TranslateProtoBlock(ProtoBlock protoBlock);

        // --- Factory ---
        private static Dictionary<BiomeID, Biome> _allBiomes;
        private static readonly object _lock = new();

        internal static Dictionary<BiomeID, Biome> GetBiomes()
        {
            lock (_lock)
            {
                if (_allBiomes != null)
                {
                    return new(_allBiomes);
                }

                var baseType = typeof(Biome);
                var assembly = Assembly.GetAssembly(baseType);

                var biomeInstances = assembly.GetTypes()
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    baseType.IsAssignableFrom(t) &&
                    t.GetConstructor(Type.EmptyTypes) != null
                )
                .Select(t => (Biome)Activator.CreateInstance(t))
                .ToList();

                Dictionary<BiomeID, Biome> targetDictionary = new();

                foreach (var biome in biomeInstances)
                {
                    string biomeName = biome.GetType().Name;
                    if (Enum.TryParse<BiomeID>(biomeName, out var biomeID))
                    {
                        targetDictionary[biomeID] = biome;
                    }
                    else
                    {
                        Core.Debug.LogError($"Biome '{biomeName}' is not defined in the BiomeList enum!");
                    }
                }

                if (Enum.GetNames(typeof(BiomeID)).Length != targetDictionary.Count)
                    throw new Exception("Biome class count and entry count in BiomeList enum don't match!");

                _allBiomes = targetDictionary;
                return targetDictionary;
            }
        }
    }
}
