using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public sealed class ElectricPipeLit : BlockServer, IElectricPipe
    {
        public ElectricPipeLit(Vec2Int POS, BlockData1 block, bool isFront) : base(POS, block, isFront) { }

        public string ELECTRIC_PIPE_ID() => typeof(ElectricPipe).Name;
        public BlockID ID_UNLIT() => BlockID.ElectricPipe;
        public BlockID ID_LIT() => BlockID.ElectricPipeLit;
    }
}
