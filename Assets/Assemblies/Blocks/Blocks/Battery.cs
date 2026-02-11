using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public sealed class Battery : BlockServer, ISolid, IElectricSource
    {
        public Battery(Vec2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public bool ALLOW_PLACE_BACK() => true;

        public ITool.Type MATERIAL_TYPE() => ITool.Type.Normal;
        public ITool.Tier MATERIAL_TIER() => ITool.Tier.Copper;

        public int STATIC_RecursionLimit(byte variant) => 16;
        public byte ElectricEmissionMask()
        {
            return 0b1111; // constant emission in all 4 directions
        }
    }
}
