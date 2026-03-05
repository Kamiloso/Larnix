using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using E = Larnix.Blocks.All.IElectricDevice;

namespace Larnix.Blocks.All
{
    public sealed class LayerRepeater : Block, ILogicGate
    {
        public byte LogicInToOut(byte input) => 0b0000;
        bool ILogicGate.EMITS_TRANSLAYER() => true;
    }
}
