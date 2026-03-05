
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using System.Collections.Generic;

namespace Larnix.Worldgen.Ores
{
    public abstract class Ore
    {

        public long BaseSeed { get; }

        public int DepthMin { get; set; } = 0;
        public int DepthMax { get; set; } = 0;

        public double OreClusterSizeCutoff { get; set; } = 0.5;

        public BlockID OreBlockId { get; set; }

        public ValueProvider OreProvider { get; set; }

        public Ore(long seed) 
        {
            BaseSeed = seed;
        }

        public virtual void GenerateOre(in ProtoBlock protoBlock,ref BlockData2 block,in Vec2Int POS,in BlockData1 oreBlock)
        {
            if (protoBlock != ProtoBlock.Stone) return;
            if (POS.y > DepthMin || POS.y < DepthMax) return;

            double oreValue = OreProvider.GetValue(POS.x, POS.y);
            if (oreValue > OreClusterSizeCutoff) 
                block = new(oreBlock, block.Back);
        }
    }
}
