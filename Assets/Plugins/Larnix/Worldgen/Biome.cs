using Larnix.Blocks;
using System.Collections;
using System.Collections.Concurrent;
using System;
using System.Reflection;
using System.Linq;

namespace Larnix.Worldgen
{
    public abstract class Biome
    {
        public abstract BlockData2 TranslateProtoBlock(ProtoBlock protoBlock);

        public static ConcurrentDictionary<BiomeID, Biome> CreateBiomeInstances()
        {
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

            ConcurrentDictionary<BiomeID, Biome> targetDictionary = new();

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

            return targetDictionary;
        }
    }
}
