using Larnix.Blocks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Larnix.Language
{
    public static class TextGetter
    {
        static readonly Dictionary<(BlockID, int), string> BlockNames = new()
        {
            {(BlockID.Dirt, 1), "Grass Block"},
            // add more as you want
        };

        public static string GetBlockName(SingleBlockData block)
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
