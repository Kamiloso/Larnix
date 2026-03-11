using System.Collections.Generic;
using Larnix.GameCore.Utils;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Worldgen.Biomes;
using Larnix.GameCore;
using System.Collections.ObjectModel;
using Larnix.Worldgen.Transformers;
using Larnix.Worldgen.Transformers.Pipeline;
using Larnix.GameCore.Enums;

namespace Larnix.Worldgen
{
    public enum ProtoBlock : ushort
    {
        Sky = 0,
        Stone = 1,
        Dirt = 2,
        Surface = 3,
        Cave = 4,
        Lake = 5
    }

    public class Generator
    {
        public Seed Seed  { get; }
        public ReadOnlyDictionary<BiomeID, Biome> Biomes { get; }
        private readonly UsefulBag _usefulBag;
        private readonly GenPipeline _genPipeline;

        public Generator(long seed)
        {
            Seed = new Seed(seed);
            Biomes = EnumFactory<BiomeID, Biome>.CreateDictionary((typeof(Seed), Seed));

            _usefulBag = new UsefulBag(this);
            UsefulBag ub = _usefulBag;

            _genPipeline = new GenPipeline
            (
                new IdentifyBiomes(ub),
                new BuildBaseTerrain(ub),
                new DrillCaves(ub),
                new ApplyHeaders(ub),
                new AddOreClusters(ub),
                new ApplyRealBlocks(ub)
            );
        }

        public ChunkData GenerateChunk(Vec2Int chunk)
        {
            BlockData2[,] blocks = _genPipeline.Run(chunk);

            ChunkData chunkData = new();
            ChunkIterator.Iterate((x, y) => chunkData[x, y] = blocks[x, y]);
            return chunkData;
        }

#region Biome & Sky Color

        public Col32 SkyColorAt(Vec2 position)
        {
            double temperature = _usefulBag.Providers["TEMPERATURE"].GetValue(position);

            Col32 arcticSkyColor = Biomes[BiomeID.Arctic].SkyColor;
            Col32 plainsSkyColor = Biomes[BiomeID.Plains].SkyColor;
            Col32 desertSkyColor = Biomes[BiomeID.Desert].SkyColor;

            return temperature switch
            {
                < -0.22 => arcticSkyColor,
                < -0.21 => Col32.Lerp(arcticSkyColor, plainsSkyColor, (temperature + 0.22) / 0.01),
                < 0.21  => plainsSkyColor,
                < 0.22  => Col32.Lerp(plainsSkyColor, desertSkyColor, (temperature - 0.21) / 0.01),
                _       => desertSkyColor
            };
        }

        public BiomeID BiomeAt(Vec2 position)
        {
            const string Phrase = "block_hash";
            double temperature = _usefulBag.Providers["TEMPERATURE"].GetValue(position.x, position.y);
            Vec2Int POS = BlockUtils.CoordsToBlock(position);

            return temperature switch
            {
                < -0.22 => BiomeID.Arctic,
                < -0.21 => Utils.ValueFromGradient(BiomeID.Arctic, BiomeID.Plains, (temperature + 0.22) / 0.01, Seed.BlockHash(POS, Phrase)),
                < 0.21  => BiomeID.Plains,
                < 0.22  => Utils.ValueFromGradient(BiomeID.Plains, BiomeID.Desert, (temperature - 0.21) / 0.01, Seed.BlockHash(POS, Phrase)),
                _       => BiomeID.Desert
            };
        }

        public Biome BiomeObjectAt(Vec2 position)
        {
            return Biomes[BiomeAt(position)];
        }

#endregion

    }
}
