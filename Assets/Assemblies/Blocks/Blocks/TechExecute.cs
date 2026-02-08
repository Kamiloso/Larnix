using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Core.Json;

namespace Larnix.Blocks
{
    public sealed class TechExecute : BlockServer, ITechExecute
    {
        public TechExecute(Vec2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        /// <summary>
        /// Configure TechExecute block with a command. Use replaceable parameters:
        /// $x, $y for block position
        /// $front for whether the block is placed in front or back (front / back)
        /// </summary>
        public static BlockData1 WithCommand(string command, BlockID replaceBlock = BlockID.Air, byte replaceVariant = 0)
        {
            Storage data = new();
            data["tech_execute.command"].String = command;
            data["tech_execute.replace.id"].Int = (int)replaceBlock;
            data["tech_execute.replace.variant"].Int = replaceVariant;

            return new BlockData1(
                id: BlockID.TechExecute,
                variant: 0,
                data: data
            );
        }
    }
}
