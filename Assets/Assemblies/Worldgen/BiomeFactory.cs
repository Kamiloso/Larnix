using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

namespace Larnix.Worldgen
{
    public class BiomeFactory
    {
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
                    t.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
                        null, Type.EmptyTypes, null) != null
                )
                .Select(t => (Biome)Activator.CreateInstance(t, true))
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

                int classCount = Enum.GetNames(typeof(BiomeID)).Length;
                int dictCount = targetDictionary.Count;

                if (classCount != dictCount)
                    throw new Exception($"Biome class count and entry count in BiomeList enum don't match! Classes: {classCount}; In dict: {dictCount}.");

                _allBiomes = targetDictionary;
                return targetDictionary;
            }
        }
    }
}
