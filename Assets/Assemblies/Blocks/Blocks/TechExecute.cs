using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Core.Json;

namespace Larnix.Blocks.All
{
    public sealed class TechExecute : Block, ITechExecute
    {
        /// <summary>
        /// Configure TechExecute block with a command. Use replaceable parameters:
        /// $x, $y for block position
        /// $front for whether the block is placed in front or back (front / back)
        /// </summary>
        public static BlockData1 WithCommand(string command, BlockData1 replaceBlock)
        {
            Storage data = new();
            data["tech_execute.command"].String = command;
            data["tech_execute.replace.id"].Int = (int)replaceBlock.ID;
            data["tech_execute.replace.variant"].Int = replaceBlock.Variant;
            data["tech_execute.replace.data"].String = replaceBlock.Data.ToString();

            return new BlockData1(
                id: BlockID.TechExecute,
                variant: 0,
                data: data
            );
        }
    }
}
