using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;
using E = Larnix.Model.Blocks.All.IElectricDevice;

namespace Larnix.Model.Blocks.All;

public sealed class LayerRepeater : Block, ILogicGate
{
    public byte LogicInToOut(byte input) => 0b0000;
    bool ILogicGate.EMITS_TRANSLAYER() => true;
}
