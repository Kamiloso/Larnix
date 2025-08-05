using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using LibNoise;
using LibNoise.Generator;
using System.Security.Cryptography;
using System;
using System.Buffers.Binary;

namespace Larnix.Server.Worldgen
{
    public class Generator
    {
        // Seed
        public readonly long Seed;

        // Raw noise
        private readonly ValueProvider NoiseSurface;
        private readonly ValueProvider NoiseCave;

        // Value providers
        private readonly ValueProvider ProviderCave;

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
            ), -15.0, 25.0, 1);

            NoiseCave = ValueProvider.CreatePerlin(new Perlin(
                frequency: 0.02,
                lacunarity: 1.8,
                persistence: 0.4,
                octaves: 6,
                seed: (int)SaltedSeed(2),
                quality: QualityMode.High
            ), -1.0, 1.0, 2);

            // ------ HELPFUL PROVIDERS ------

            ValueProvider _SurfaceRelative = ValueProvider.CreateFunction((x, y, z) =>
            {
                return y - NoiseSurface.GetValue(x);
            });

            // ------ CAVE PROVIDER ------

            ValueProvider _DryCheck_CAVES = ValueProvider.CreateCondition(NoiseSurface, 8.0, double.MaxValue, 4.0, true);
            ValueProvider _UndergroundCheck_CAVES = ValueProvider.CreateCondition(_SurfaceRelative, -4.0, double.MaxValue, 16.0, false);

            ValueProvider _Condition_CAVES = ValueProvider.CreateOr(new List<ValueProvider>{
                _DryCheck_CAVES, _UndergroundCheck_CAVES
            });

            ProviderCave = ValueProvider.CreateFunction((x, y, z) =>
                (NoiseCave.GetValue(x, y) + 1.0) * _Condition_CAVES.GetValue(x, y) - 1.0);
        }

        public BlockData[,] GenerateChunk(Vector2Int chunk)
        {
            BlockData[,] blocks = new BlockData[16, 16];

            // Base terrain

            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    Vector2Int POS = ChunkMethods.GlobalBlockCoords(chunk, new Vector2Int(x, y));

                    const int DIRT_LAYER_SIZE = 3;

                    int surface_level = Mathf.FloorToInt((float)NoiseSurface.GetValue(POS.x));
                    int stone_level = surface_level - DIRT_LAYER_SIZE;

                    if (POS.y > surface_level) // air
                    {
                        bool is_lake = POS.y <= WATER_LEVEL;

                        blocks[x, y] = new BlockData(
                            new SingleBlockData { ID = is_lake ? BlockID.Water : BlockID.Air },
                            new SingleBlockData {  }
                            );
                    }
                    else if (POS.y > stone_level) // dirt
                    {
                        bool is_top = POS.y + 1.0 > surface_level;

                        blocks[x, y] = new BlockData(
                            new SingleBlockData { ID = BlockID.Dirt, Variant = (byte)(is_top ? 1 : 0) },
                            new SingleBlockData {  }
                            );
                    }
                    else // underground
                    {
                        blocks[x, y] = new BlockData(
                            new SingleBlockData { ID = BlockID.Stone },
                            new SingleBlockData { ID = BlockID.Stone }
                            );
                    }
                }

            // Cave generation

            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    Vector2Int POS = ChunkMethods.GlobalBlockCoords(chunk, new Vector2Int(x, y));

                    const double CAVE_NOISE_WIDTH = 0.2f;
                    double cave_value = ProviderCave.GetValue(POS.x, POS.y);

                    if (cave_value > -CAVE_NOISE_WIDTH && cave_value < CAVE_NOISE_WIDTH) // cave
                    {
                        blocks[x, y].Front = new SingleBlockData { };
                    }
                }

            return blocks;
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
    }
}
