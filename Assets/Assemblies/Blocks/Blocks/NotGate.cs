using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using E = Larnix.Blocks.IElectricDevice;

namespace Larnix.Blocks
{
    public sealed class NotGate : BlockServer, ISolidElectric, ILogicGate
    {
        public NotGate(Vec2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public byte LogicInToOut(byte input)
        {
            bool down = (input & E.DOWN) != 0;
            return (byte)(down ? 0 : E.UP);
        }
    }
}
