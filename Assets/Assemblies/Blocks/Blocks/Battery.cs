using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public sealed class Battery : BlockServer, ISolidElectric, IElectricSource
    {
        public Battery(Vec2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public byte ElectricEmissionMask() => 0b1111;
    }
}
