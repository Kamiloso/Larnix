using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Text;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Worldgen.Noise;
using Random = System.Random;
using Larnix.Blocks;
using NUnit.Framework;
using Larnix.Worldgen.Biomes;
using Larnix.Worldgen.Ores;
using Larnix.Worldgen.Biomes.Interfaces;

namespace Larnix.Worldgen
{
    public enum ProtoBlock { Air, Stone, Soil, SoilSurface, Cave, Liquid }
    public class Generator
    {
        private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;

        // Variables
        private readonly Seed _seed;
        private readonly Dictionary<BiomeID, Biome> _biomes;
        private readonly Dictionary<OreID, Ore> _ores;
        public long Seed => _seed.Value;

        // Raw noise
        private readonly ValueProvider NoiseSurface;
        private readonly ValueProvider NoiseCave;
        private readonly ValueProvider NoiseTemperature;

        // Value providers
        private readonly ValueProvider ProviderSurface;
        private readonly ValueProvider ProviderCave;
        private readonly ValueProvider ProviderTemperature;

        // Constants
        const int WATER_LEVEL = -1;
        const int SOIL_LAYER_SIZE = 3;

        public Generator(long seed)
        {
            _biomes = BiomeFactory.GetBiomes();
            _ores = OreFactory.GetOres(seed);
            _seed = new Seed(seed);

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

            // ------ TEMPERATURE PROVIDER ------

            //ProviderTemperature = ValueProvider.CreateFunction((x, y, z) =>
            //    _SurfaceRelative.GetValue(x, y, z));
        }

        public Col32 SkyColorAt(Vec2 position)
        {
            return new Col32(197, 235, 255, 0);
        }

        public BiomeID BiomeAt(Vec2 position)
        {
            const string PHRASE = "block_hash";
            BiomeID biomeID;
            double temperature = NoiseTemperature.GetValue(position.x, position.y);

            Vec2Int IntPos = new Vec2Int((int)position.x, (int)position.y);

            if (temperature < -0.22)
                biomeID = BiomeID.Arctic;

            else if (temperature < -0.21)
            {
                double gradient = (temperature - (-0.22)) / 0.01;
                biomeID = ValueFromGradient(BiomeID.Arctic, BiomeID.Plains, gradient, _seed.BlockHash(IntPos, PHRASE));
            }
            else if (temperature < 0.21)
                biomeID = BiomeID.Plains;

            else if (temperature < 0.22)
            {
                double gradient = (temperature - 0.21) / 0.01;
                biomeID = ValueFromGradient(BiomeID.Plains, BiomeID.Desert, gradient, _seed.BlockHash(IntPos, PHRASE));
            }
            else
                biomeID = BiomeID.Desert;

            return biomeID;
        }

        public BlockData2[,] GenerateChunk(Vec2Int chunk)
        {
            ProtoBlock[,] protoBlocks = ChunkIterator.Array2D<ProtoBlock>();

            BuildBaseTerrain(protoBlocks, chunk);
            BuildCaves(protoBlocks, chunk);

            BlockData2[,] blocks = ConvertToBlockArray(protoBlocks, chunk);

            AddOreClusters(protoBlocks, blocks, chunk);

            return blocks.DeepCopyChunk();
        }

        private void BuildBaseTerrain(ProtoBlock[,] protoBlocks, Vec2Int chunk)
        {
            ChunkIterator.Iterate((x, y) =>
            {
                var pos = new Vec2Int(x, y);

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
            }
        }

        private void BuildCaves(ProtoBlock[,] protoBlocks, Vec2Int chunk)
        {
            foreach (Vec2Int pos in ChunkIterator.IterateXY())
            {
                var pos = new Vec2Int(x, y);

                Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, pos);

                const double CAVE_NOISE_WIDTH = 0.2f;
                double cave_value = ProviderCave.GetValue(POS.x, POS.y);

                if (cave_value > -CAVE_NOISE_WIDTH && cave_value < CAVE_NOISE_WIDTH) // cave
                {
                    ProtoBlock protoBlock = protoBlocks[x, y];
                    protoBlock = protoBlock switch
                    {
                        ProtoBlock.Stone => ProtoBlock.Cave,
                        _ => ProtoBlock.Air,
                    };
                    protoBlocks[x, y] = protoBlock;
                }
            }
        }

        private void AddOreClusters(ProtoBlock[,] protoBlocks, BlockData2[,] blocks, Vec2Int chunk)
        {
            Vec2Int chunkStart = BlockUtils.GlobalBlockCoords(chunk, new Vec2Int(CHUNK_SIZE, CHUNK_SIZE));
            Vec2Int chunkEnd = BlockUtils.GlobalBlockCoords(chunk, new Vec2Int(0, 0));

            foreach (Vec2Int pos in ChunkIterator.IterateXY())
            {
                var pos = new Vec2Int(x, y);

                Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, pos);

                BiomeID biomeID = BiomeAt(POS.ToVec2());

                ProtoBlock protoBlock = protoBlocks[x, y];
                Biome biome = _biomes[biomeID];

                var biomeWithOre = biome as IHasOre;

                if (biomeWithOre != null)
                {
                    foreach (var oreID in biomeWithOre.ORES().Keys)
                    {
                        Ore ore = _ores[oreID];

                        if (ore.DepthMax > chunkStart.y || ore.DepthMin < chunkEnd.y) continue;
                        BlockData1 block = biomeWithOre.STATIC_GetOreBlock(oreID, ore.OreBlockId);

                        ore.GenerateOre(protoBlock, ref blocks[x, y], POS, block);
                    }
                }
            });
        }

        private BlockData2[,] ConvertToBlockArray(ProtoBlock[,] protoBlocks, Vec2Int chunk)
        {
            BlockData2[,] blocks = ChunkIterator.Array2D<BlockData2>();

            ChunkIterator.Iterate((x, y) =>
            {
                var pos = new Vec2Int(x, y);
                
                Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, pos);

                BiomeID biomeID = BiomeAt(POS.ToVec2());

                Biome biome = _biomes[biomeID];
                blocks[x, y] = biome.TranslateProtoBlock(protoBlocks[x, y]);
            });

            return blocks;
        }

        private T ValueFromGradient<T>(T value1, T value2, double gradient, long entropySource)
        {
            gradient = Math.Clamp(gradient, 0.0, 1.0);

            Random rng = new Random((int)(entropySource ^ (entropySource >> 32)));
            double roll = rng.NextDouble();

            return roll < gradient ? value2 : value1;
        }

        public string GetNoiseInfo(Vec2Int position)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Noise at [{position.x}, {position.y}]:");

            FieldInfo[] fields = this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(ValueProvider))
                {
                    ValueProvider vp = field.GetValue(this) as ValueProvider;
                    if (vp != null)
                    {
                        double val = vp.GetValue(position.x, position.y);
                        sb.AppendLine($"{field.Name}: {val}");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
