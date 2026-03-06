using Larnix.Blocks.Structs;

namespace Larnix.Worldgen.Biomes
{
    public abstract class Biome
    {
        public Seed Seed { get; private init; }

        protected Biome(Seed seed)
        {
            Seed = seed;
        }

        public abstract BlockData2 TranslateProtoBlock(ProtoBlock protoBlock);
    }
}
