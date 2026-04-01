using Larnix.Model.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;
using E = Larnix.Model.Blocks.All.IElectricDevice;

namespace Larnix.Model.Blocks.All;

public sealed class AndGate : Block, ISolidElectric, ILogicGate
{
    public byte LogicInToOut(byte input)
    {
        bool left = (input & E.LEFT) != 0;
        bool right = (input & E.RIGHT) != 0;
        return (byte)(left && right ? E.UP : 0);
    }
}
