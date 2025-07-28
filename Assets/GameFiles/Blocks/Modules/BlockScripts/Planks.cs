using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Server.Terrain;

namespace Larnix.Modules.Blocks
{
    public class Planks : BlockServer
    {
        public Planks(Vector2Int POS, SingleBlockData block, bool isFront) : base(POS, block, isFront) { }
    }
}
