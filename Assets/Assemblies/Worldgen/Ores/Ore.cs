using System;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Blocks.All;

namespace Larnix.Worldgen.Ores
{
    public abstract class Ore
    {
        public bool FrontEnabled { get; init; } = true;
        public bool BackEnabled { get; init; } = false;

        public Func<BlockData1, BlockData1> BlockTransform { private get; init; } =
            _ => new(BlockID.Bedrock, 0); // no ore defined => bedrock

        public Ore() { }

        public abstract bool OrePresentAt(Vec2Int POS);

        public bool TryGenerateOre(Vec2Int POS, BlockData1 oldBlock, out BlockData1 newBlock)
        {
            if (BlockFactory.HasInterface<IOreReplaceable>(oldBlock.ID) &&
                OrePresentAt(POS))
            {
                newBlock = BlockTransform(oldBlock);
                return true;
            }

            newBlock = default;
            return false;
        }
    }
}
