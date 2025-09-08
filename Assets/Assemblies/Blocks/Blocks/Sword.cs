using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Blocks
{
    public class Sword : BlockServer, IBlockInterface
    {
        public Sword(Vector2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }
    }
}
