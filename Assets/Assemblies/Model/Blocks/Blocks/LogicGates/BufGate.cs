using Larnix.Model.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;
using E = Larnix.Model.Blocks.All.IElectricDevice;

namespace Larnix.Model.Blocks.All;

public sealed class BufGate : Block, ISolidElectric, ILogicGate
{
    public byte LogicInToOut(byte input)
    {
        bool down = (input & E.DOWN) != 0;
        return (byte)(down ? E.UP : 0);
    }
}
