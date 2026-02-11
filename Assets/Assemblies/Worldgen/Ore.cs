
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;

namespace Larnix.Worldgen
{
    public abstract class Ore
    {

        public long BaseSeed { get; }

        public int DepthMin { get; set; } = 0;
        public int DepthMax { get; set; } = 0;

        public double OreClusterSizeCutoff { get; set; } = 0.5;

        public BlockData1 OreFront { get; set; }

        public ValueProvider OreProvider { get; set; }

        public Ore(long seed) 
        {
            BaseSeed = seed;
        }

        public virtual void GenerateOre(in ProtoBlock[,] protoBlocks, BlockData2[,] blocks,in Vec2Int chunk)
        {
            foreach (Vec2Int pos in ChunkIterator.IterateXY())
            {
                int x = pos.x;
                int y = pos.y;

                Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, pos);

                ProtoBlock protoBlock = protoBlocks[x, y];

                if (protoBlock != ProtoBlock.Stone) continue;
                if (POS.y > DepthMin || POS.y < DepthMax) continue;

                double oreValue = OreProvider.GetValue(POS.x, POS.y);
                if (oreValue > OreClusterSizeCutoff) 
                {
                    BlockData2 newBlock = new(OreFront, blocks[x, y].Back);
                    blocks[x, y] = newBlock;
                }
            }
        }
    }
}
