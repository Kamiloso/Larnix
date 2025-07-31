using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Server.Terrain;

namespace Larnix.Modules.Blocks
{
    public class Stone : BlockServer, ISolid, IPlaceable, IBreakable
    {
        public Stone(Vector2Int POS, SingleBlockData block, bool isFront) : base(POS, block, isFront) { }

        public bool ALLOW_PLACE_BACK() => true;

        public ITool.Type MATERIAL_TYPE() => ITool.Type.Surface;
        public ITool.Tier MATERIAL_TIER() => ITool.Tier.Wood;
    }
}
