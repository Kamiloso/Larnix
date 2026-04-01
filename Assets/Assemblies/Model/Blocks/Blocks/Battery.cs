using Larnix.Model.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Model.Blocks.All;

public sealed class Battery : Block, ISolidElectric, IElectricSource
{
    public byte ElectricEmissionMask() => 0b1111;
}
