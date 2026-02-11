
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;

namespace Larnix.Worldgen.Ores
{
    public class Empty : Ore
    {
        public Empty(long seed) : base(seed) { }

        public override void GenerateOre(in ProtoBlock[,] protoBlocks, BlockData2[,] blocks, in Vec2Int chunk){}
    }
}
