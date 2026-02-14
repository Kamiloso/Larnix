
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using System.Collections.Generic;

namespace Larnix.Worldgen
{
    public abstract class Ore
    {

        public long BaseSeed { get; }

        public int DepthMin { get; set; } = 0;
        public int DepthMax { get; set; } = 0;

        public bool VariantBased { get; set; } = true;

        public double OreClusterSizeCutoff { get; set; } = 0.5;

        public BlockID OreBlockId { get; set; }

        public ValueProvider OreProvider { get; set; }

        public Dictionary<BiomeID, BlockData1> BlockByBiome { get; set; }

        public Ore(long seed) 
        {
            BaseSeed = seed;
        }

        public virtual void GenerateOre(in ProtoBlock protoBlock,ref BlockData2 block,in Vec2Int POS, BiomeID biomeID)
        {
            if (protoBlock != ProtoBlock.Stone) return;
            if (POS.y > DepthMin || POS.y < DepthMax) return;

            double oreValue = OreProvider.GetValue(POS.x, POS.y);
            if (oreValue > OreClusterSizeCutoff) 
            {
                if (VariantBased) 
                {
                    block = new(new(OreBlockId, (byte)biomeID), block.Back);
                }
                else
                {
                    if (BlockByBiome.ContainsKey(biomeID))
                        block = new(BlockByBiome[biomeID], block.Back);
                }
            }
        }
    }
}
