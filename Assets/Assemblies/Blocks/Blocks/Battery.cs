using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public sealed class Battery : Block, ISolidElectric, IElectricSource
    {
        public byte ElectricEmissionMask() => 0b1111;
    }
}
