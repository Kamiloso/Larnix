using System;
using System.Collections.Generic;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Worldgen.Biomes.All;

namespace Larnix.Worldgen.Ores
{
    internal abstract class Ore
    {
        protected Seed Seed { get; private init; }
        protected ValueProvider OreProvider { get; init; }
        public OreID ID => Enum.Parse<OreID>(GetType().Name);

        public abstract BlockData1 DefaultBlock { get; }
        public abstract double OreClusterSizeCutoff { get; }
        public virtual ProtoBlock BaseProtoBlock => ProtoBlock.Stone;
        public virtual int MaxHeight => int.MaxValue;
        public virtual int MinHeight => int.MinValue;
        
        public Ore(Seed seed)
        {
            Seed = seed;
        }

        public bool ShouldGenerateOre(Vec2Int POS, ProtoBlock protoBlock)
        {
            if (protoBlock != BaseProtoBlock) return false;
            if (POS.y < MinHeight || POS.y > MaxHeight) return false;

            double value = OreProvider?.GetValue(POS.x, POS.y) ?? 0.0;
            return value > OreClusterSizeCutoff;
        }

        public BlockData1 GenerateOreWith(IHasOre iface)
        {
            var ores = iface.ORES();
            if (ores.TryGetValue(ID, out BlockData1 blockData) && blockData != null)
                return blockData.DeepCopy();
            
            return DefaultBlock;
        }
    }
}
