using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using E = Larnix.Blocks.All.IElectricDevice;

namespace Larnix.Blocks.All
{
    public sealed class XnorGate : Block, ISolidElectric, ILogicGate
    {
        public byte LogicInToOut(byte input)
        {
            bool left = (input & E.LEFT) != 0;
            bool right = (input & E.RIGHT) != 0;
            return (byte)(left ^ right ? 0 : E.UP);
        }
    }
}
