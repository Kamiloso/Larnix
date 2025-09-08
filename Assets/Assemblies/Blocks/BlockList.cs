using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Blocks
{
    public enum BlockLayer : byte { Front, Back }
    public enum BlockID : ushort
    {
        Air = 0,
        Stone = 1,
        Soil = 2,
        Planks = 3,
        Water = 4,
        Lava = 5,
        Sand = 6,
        Log = 7,
        Leaves = 8,
        Ice = 9,
        Snow = 10,
        CrudeOil = 11,
        MiningTool = 12,
        Sandstone = 13,
        Glass = 14,
        Plastic = 15,
        Sword = 16,
    }
}
