using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using E = Larnix.Blocks.All.IElectricDevice;

namespace Larnix.Blocks.All
{
    public sealed class BufGate : Block, ISolidElectric, ILogicGate
    {
        public byte LogicInToOut(byte input)
        {
            bool down = (input & E.DOWN) != 0;
            return (byte)(down ? E.UP : 0);
        }
    }
}
