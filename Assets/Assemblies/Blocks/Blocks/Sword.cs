using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public sealed class Sword : BlockServer, IBlockInterface
    {
        public Sword(Vec2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }
    }
}
