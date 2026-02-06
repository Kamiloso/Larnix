using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public sealed class Soil : BlockServer, ISolid, IPlaceable, IBreakable, IHasGrowingFlora, IFalling
    {
        public Soil(Vec2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public bool ALLOW_PLACE_BACK() => false;

        public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
        public ITool.Tier MATERIAL_TIER() => ITool.Tier.None;

        public double DRY_CHANCE() => 0.001;
        public double GROWTH_CHANCE() => 0.0002;
        public int FALL_PERIOD() => 5;

        string IBlockInterface.STATIC_GetBlockName(byte variant)
        {
            return variant switch
            {
                1 => "Grassy Soil",
                2 => "Hemo Soil",
                _ => ((IBlockInterface)this).STATIC_GetBlockNameFallback(variant)
            };
        }
    }
}
