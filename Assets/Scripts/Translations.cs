using Larnix.Blocks;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Larnix
{
    public static class Translations
    {
        static readonly ConcurrentDictionary<(BlockID, int), string> BlockNames = new();

        static Translations()
        {
            BlockNames.TryAdd((BlockID.Soil, 1), "Grassy Soil");
            BlockNames.TryAdd((BlockID.Soil, 2), "Hemo Soil");
            // add more as you want
        }

        public static string GetBlockName(BlockData1 block)
        {
            if (BlockNames.TryGetValue((block.ID, block.Variant), out var name))
                return name;

            return SplitPascalCase(block.ID.ToString());
        }

        public static string SplitPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var step1 = Regex.Replace(input, @"([A-Z])(?=[A-Z][a-z])", "$1 ");
            return Regex.Replace(step1, @"(?<=[a-z])(?=[A-Z])", " ");
        }
    }
}
