using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using Larnix.Blocks;
using LibNoise;
using LibNoise.Generator;
using System.Security.Cryptography;
using System;
using System.Buffers.Binary;
using System.Reflection;
using System.Text;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;

namespace Larnix.Worldgen
{
    public enum ProtoBlock : ushort
    {
        Air,
        Stone,
        Soil,
        SoilSurface,
        Cave,
        Liquid,
    }

    public class Generator
    {
        // Variables
        public readonly long Seed;

        // Raw noise
        private readonly ValueProvider NoiseSurface;
        private readonly ValueProvider NoiseCave;
        private readonly ValueProvider NoiseTemperature;

        // Value providers
        private readonly ValueProvider ProviderSurface;
        private readonly ValueProvider ProviderCave;
        private readonly ValueProvider ProviderTemperature;

        // Data structures
        private static readonly ConcurrentDictionary<BiomeID, Biome> Biomes = Biome.CreateBiomeInstances();

        // Constants
        const int WATER_LEVEL = -1;

        public Generator(long seed)
        {
            Seed = seed;

            // ------ BASE NOISES ------

            NoiseSurface = ValueProvider.CreatePerlin(new Perlin(
                frequency: 0.01,
                lacunarity: 2.0,
                persistence: 0.5,
                octaves: 4,
                seed: (int)SaltedSeed(1),
                quality: QualityMode.High
            ), -20.0, 30.0, 1);

            NoiseCave = ValueProvider.CreatePerlin(new Perlin(
                frequency: 0.02,
                lacunarity: 1.8,
                persistence: 0.4,
                octaves: 3,
                seed: (int)SaltedSeed(2),
                quality: QualityMode.High
            ), -1.0, 1.0, 2).Stretch(1.25, 0.75);

            NoiseTemperature = ValueProvider.CreatePerlin(new Perlin(
                frequency: 0.0015,
                lacunarity: 1.8,
                persistence: 0.4,
                octaves: 1,
                seed: (int)SaltedSeed(3),
                quality: QualityMode.High
            ), -1.0, 1.0, 2);

            // ------ SURFACE PROVIDER ------

            ProviderSurface = ValueProvider.CreateFunction((x, y, z) =>
            {
                double val = NoiseSurface.GetValue(x, y, z);
                return val > 0.0 ? val : val / 2.0;
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

        public Color SkyColorAt(Vector2 position)
        {
            return new Color32(105, 165, 255, 0);
        }

        public BlockData2[,] GenerateChunk(Vector2Int chunk)
        {
            ProtoBlock[,] protoBlocks = new ProtoBlock[16, 16];

            Build_BaseTerrain(protoBlocks, chunk);
            Build_Caves(protoBlocks, chunk);

            BlockData2[,] blocks = ConvertToBlockArray(protoBlocks, chunk);

            //Add_Ores();
            //Add_Vegetation();

            return blocks;
        }

        private void Build_BaseTerrain(ProtoBlock[,] protoBlocks, Vector2Int chunk)
        {
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    Vector2Int POS = BlockUtils.GlobalBlockCoords(chunk, new Vector2Int(x, y));

                    const int SOIL_LAYER_SIZE = 3;

                    int surface_level = Mathf.FloorToInt((float)ProviderSurface.GetValue(POS.x));
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

        private void Build_Caves(ProtoBlock[,] protoBlocks, Vector2Int chunk)
        {
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    Vector2Int POS = BlockUtils.GlobalBlockCoords(chunk, new Vector2Int(x, y));

                    const double CAVE_NOISE_WIDTH = 0.2f;
                    double cave_value = ProviderCave.GetValue(POS.x, POS.y);

                    if (cave_value > -CAVE_NOISE_WIDTH && cave_value < CAVE_NOISE_WIDTH) // cave
                    {
                        ProtoBlock protoBlock = protoBlocks[x, y];
                        switch(protoBlock)
                        {
                            case ProtoBlock.Stone: protoBlock = ProtoBlock.Cave; break;
                            default: protoBlock = ProtoBlock.Air; break;
                        }
                        protoBlocks[x, y] = protoBlock;
                    }
                }
        }

        private BlockData2[,] ConvertToBlockArray(ProtoBlock[,] protoBlocks, Vector2Int chunk)
        {
            BlockData2[,] blocks = new BlockData2[16, 16];

            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    Vector2Int POS = BlockUtils.GlobalBlockCoords(chunk, new Vector2Int(x, y));

                    BiomeID biomeID = BiomeID.Empty;
                    double temperature = NoiseTemperature.GetValue(POS.x, POS.y);

                    if (temperature < -0.22)
                        biomeID = BiomeID.Arctic;

                    else if (temperature < -0.21 )
                    {
                        double gradient = (temperature - (-0.22)) / 0.01;
                        biomeID = ValueFromGradient(BiomeID.Arctic, BiomeID.Plains, gradient, BlockHash(POS, 1));
                    }

                    else if (temperature < 0.21)
                        biomeID = BiomeID.Plains;

                    else if (temperature < 0.22)
                    {
                        double gradient = (temperature - 0.21) / 0.01;
                        biomeID = ValueFromGradient(BiomeID.Plains, BiomeID.Desert, gradient, BlockHash(POS, 1));
                    }

                    else if(true)
                        biomeID = BiomeID.Desert;

                    Biome biome = Biomes[biomeID];
                    blocks[x, y] = biome.TranslateProtoBlock(protoBlocks[x, y]);
                }

            return blocks;
        }

        private T ValueFromGradient<T>(T value1, T value2, double gradient, long entropySource)
        {
            gradient = System.Math.Clamp(gradient, 0.0, 1.0);

            System.Random rng = new System.Random((int)(entropySource ^ (entropySource >> 32)));
            double roll = rng.NextDouble();

            return roll < gradient ? value2 : value1;
        }

        private long BlockHash(Vector2Int POS, long salt)
        {
            Span<byte> buffer = stackalloc byte[8 + 4 + 4 + 8];

            BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(0, 8), Seed);
            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(8, 4), POS.x);
            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(12, 4), POS.y);
            BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(16, 8), salt);

            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(buffer.ToArray());

            return BinaryPrimitives.ReadInt64BigEndian(hash.AsSpan(0, 8));
        }

        private long SaltedSeed(long salt)
        {
            Span<byte> buffer = stackalloc byte[8 + 8];

            BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(0, 8), Seed);
            BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(8, 8), salt);

            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(buffer.ToArray());

            return BinaryPrimitives.ReadInt64BigEndian(hash.AsSpan(0, 8));
        }

        public string GetNoiseInfo(Vector2Int position)
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
