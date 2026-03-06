using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

namespace Larnix.Worldgen.Ores
{
    internal class OreFactory
    {
        public static Dictionary<OreID, Ore> CreateOres(Seed seed)
        {
            var baseType = typeof(Ore);
            var assembly = Assembly.GetAssembly(baseType);
            Type[] types = { typeof(Seed) };

            var oreInstances = assembly.GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                baseType.IsAssignableFrom(t) &&
                t.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
                    null, types, null) != null
            )
            .Select(t => (Ore)Activator.CreateInstance(t, seed))
            .ToList();


            Dictionary<OreID, Ore> targetDictionary = new();

            foreach (var ore in oreInstances)
            {
                string oreName = ore.GetType().Name;
                if (Enum.TryParse<OreID>(oreName, out var oreID))
                {
                    targetDictionary[oreID] = ore;
                }
                else
                {
                    Core.Debug.LogError($"Ore '{oreName}' is not defined in the OreList enum!");
                }
            }

            int classCount = Enum.GetNames(typeof(OreID)).Length;
            int dictCount = targetDictionary.Count;

            if (classCount != dictCount)
                throw new Exception($"Ore class count and entry count in OreList enum don't match! Classes: {classCount}; In dict: {dictCount}.");

            return targetDictionary;
        }
    }
}
