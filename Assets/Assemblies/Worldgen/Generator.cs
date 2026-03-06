using System.Collections.Generic;
using System;
using System.Reflection;
using System.Text;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Worldgen.Noise;
using Larnix.Blocks;
using Larnix.Worldgen.Biomes;
using Larnix.Worldgen.Ores;
using Larnix.Worldgen.Biomes.All;
using Larnix.Core;

namespace Larnix.Worldgen
{
    public enum ProtoBlock { Air, Stone, Soil, SoilSurface, Cave, Liquid }
    public class Generator
    {
        private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;

        // Variables
        private readonly Seed _seed;
        private readonly Dictionary<BiomeID, Biome> _biomes;
        public long Seed => _seed.Value;

        // Raw noise
        private readonly ValueProvider NoiseSurface;
        private readonly ValueProvider NoiseCave;
        private readonly ValueProvider NoiseTemperature;

        // Value providers
        private readonly ValueProvider ProviderSurface;
        private readonly ValueProvider ProviderCave;

        // Constants
        const int WATER_LEVEL = -1;
        const int SOIL_LAYER_SIZE = 3;

        public Generator(long seed)
        {
            _seed = new Seed(seed);
            _biomes = EnumFactory<BiomeID, Biome>.CreateDictionary((typeof(Seed), _seed));

            // ------ BASE NOISES ------

            NoiseSurface = ValueProvider.CreatePerlin(
                new Perlin(seed: (int)_seed.Hash("noise_surface"))
                {
                    Octaves = 4,
                    Frequency = 0.013,
                    Lacunarity = 2.0,
                    Persistence = 0.3,
                },
                min: -25.0, max: 40.0, dim: 1);

            NoiseCave = ValueProvider.CreatePerlin(
                new Perlin(seed: (int)_seed.Hash("noise_cave"))
                {
                    Octaves = 3,
                    Frequency = 0.025,
                    Lacunarity = 1.8,
                    Persistence = 0.3,
                },
                min: -1.0, max: 1.0, dim: 2).Stretch(1.25, 0.75);

            NoiseTemperature = ValueProvider.CreatePerlin(
                new Perlin(seed: (int)_seed.Hash("noise_temperature"))
                {
                    Octaves = 1,
                    Frequency = 0.0015,
                    Lacunarity = 1.8,
                    Persistence = 0.4,
                },
                min: -1.0, max: 1.0, dim: 2);

            // ------ SURFACE PROVIDER ------

            ProviderSurface = ValueProvider.CreateFunction((x, y, z) =>
            {
                double val = NoiseSurface.GetValue(x, y, z);
                return val > 0.0 ? val : val * 2.0 / 3.0;
            });

            ValueProvider _SurfaceRelative = ValueProvider.CreateFunction((x, y, z) =>
            {
                return y - ProviderSurface.GetValue(x);
            });

            // ------ CAVE PROVIDER ------

            ValueProvider _DryCheck_CAVES = ValueProvider.CreateCondition(ProviderSurface, double.MinValue, 2.0, 10.0).Negate();
            ValueProvider _UndergroundCheck_CAVES = ValueProvider.CreateCondition(_SurfaceRelative, -4.0, double.MaxValue, 30.0).Negate();

            ValueProvider _Condition_CAVES = _DryCheck_CAVES.Or(_UndergroundCheck_CAVES);

            ProviderCave = ValueProvider.CreateFunction((x, y, z) =>
                (NoiseCave.GetValue(x, y) + 1.0) * _Condition_CAVES.GetValue(x, y) - 1.0);
        }

        private static readonly Col32 ArcticSkyColor = new(200, 240, 255, 0);
        private static readonly Col32 PlainsSkyColor = new(135, 206, 235, 0);
        private static readonly Col32 DesertSkyColor = new(80,  180, 250, 0);

        public Col32 SkyColorAt(Vec2 position)
        {
            double temperature = NoiseTemperature.GetValue(position.x, position.y);

            return temperature switch
            {
                < -0.22 => ArcticSkyColor,
                < -0.21 => Col32.Lerp(ArcticSkyColor, PlainsSkyColor, (temperature + 0.22) / 0.01),
                < 0.21  => PlainsSkyColor,
                < 0.22  => Col32.Lerp(PlainsSkyColor, DesertSkyColor, (temperature - 0.21) / 0.01),
                _       => DesertSkyColor
            };
        }

        public BiomeID BiomeAt(Vec2 position)
        {
            const string Phrase = "block_hash";
            double temperature = NoiseTemperature.GetValue(position.x, position.y);
            Vec2Int POS = BlockUtils.CoordsToBlock(position);

            return temperature switch
            {
                < -0.22 => BiomeID.Arctic,
                < -0.21 => Utils.ValueFromGradient(BiomeID.Arctic, BiomeID.Plains, (temperature + 0.22) / 0.01, _seed.BlockHash(POS, Phrase)),
                < 0.21  => BiomeID.Plains,
                < 0.22  => Utils.ValueFromGradient(BiomeID.Plains, BiomeID.Desert, (temperature - 0.21) / 0.01, _seed.BlockHash(POS, Phrase)),
                _       => BiomeID.Desert
            };
        }

        public BlockData2[,] GenerateChunk(Vec2Int chunk)
        {
            ProtoBlock[,] protoBlocks = ChunkIterator.Array2D<ProtoBlock>();

            BuildBaseTerrain(protoBlocks, chunk);
            BuildCaves(protoBlocks, chunk);

            BlockData2[,] blocks = ConvertToBlockArray(protoBlocks, chunk);

            AddOreClusters(blocks, chunk);

            return blocks.DeepCopyChunk();
        }

        private void BuildBaseTerrain(ProtoBlock[,] protoBlocks, Vec2Int chunk)
        {
            ChunkIterator.Iterate((x, y) =>
            {
                Vec2Int pos = new(x, y);
                Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, pos);

                int surface_level = (int)Math.Floor(ProviderSurface.GetValue(POS.x));
                int stone_level = surface_level - SOIL_LAYER_SIZE;

                if (POS.y > surface_level) // air
                {
                    bool is_lake = POS.y <= WATER_LEVEL;
                    protoBlocks[x, y] = is_lake ? ProtoBlock.Liquid : ProtoBlock.Air;
                }
                else if (POS.y > stone_level) // dirt
                {
                    bool is_top = POS.y + 1 > surface_level;
                    protoBlocks[x, y] = is_top ? ProtoBlock.SoilSurface : ProtoBlock.Soil;
                }
                else // underground
                {
                    protoBlocks[x, y] = ProtoBlock.Stone;
                }
            });
        }

        private void BuildCaves(ProtoBlock[,] protoBlocks, Vec2Int chunk)
        {
            ChunkIterator.Iterate((x, y) =>
            {
                Vec2Int pos = new(x, y);
                Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, pos);

                const double CAVE_NOISE_WIDTH = 0.2f;
                double cave_value = ProviderCave.GetValue(POS.x, POS.y);

                if (cave_value > -CAVE_NOISE_WIDTH && cave_value < CAVE_NOISE_WIDTH) // cave
                {
                    protoBlocks[x, y] = protoBlocks[x, y] == ProtoBlock.Stone ?
                        ProtoBlock.Cave : ProtoBlock.Air;
                }
            });
        }

        private void AddOreClusters(BlockData2[,] blocks, Vec2Int chunk)
        {
            Vec2Int chunkStart = BlockUtils.GlobalBlockCoords(chunk, new Vec2Int(CHUNK_SIZE, CHUNK_SIZE));
            Vec2Int chunkEnd = BlockUtils.GlobalBlockCoords(chunk, new Vec2Int(0, 0));

            ChunkIterator.Iterate((x, y) =>
            {
                Vec2Int pos = new(x, y);
                Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, pos);

                BlockData1 oldBlock = blocks[x, y].Front;

                BiomeID biomeID = BiomeAt(POS.ToVec2());
                Biome biome = _biomes[biomeID];

                if (biome is IHasOre iface)
                {
                    foreach (Ore ore in iface.ORES())
                    {
                        if (ore.TryGenerateOre(POS, oldBlock, out var newBlock))
                        {
                            blocks[x, y] = new BlockData2(newBlock, blocks[x, y].Back);
                        }
                    }
                }
            });
        }

        private BlockData2[,] ConvertToBlockArray(ProtoBlock[,] protoBlocks, Vec2Int chunk)
        {
            BlockData2[,] blocks = ChunkIterator.Array2D<BlockData2>();

            ChunkIterator.Iterate((x, y) =>
            {
                Vec2Int pos = new(x, y);
                Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, pos);

                BiomeID biomeID = BiomeAt(POS.ToVec2());

                Biome biome = _biomes[biomeID];
                blocks[x, y] = biome.TranslateProtoBlock(protoBlocks[x, y]);
            });

            return blocks;
        }
    }
}
