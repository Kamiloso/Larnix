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
        public readonly long Seed;
        
        private readonly Perlin NoiseSurface;

        public Generator(long seed)
        {
            Seed = seed;

            NoiseSurface = new Perlin(
                frequency: 0.01,
                lacunarity: 2.0,
                persistence: 0.5,
                octaves: 4,
                seed: (int)SaltedSeed(1),
                quality: QualityMode.High
                );
        }

        public BlockData[,] GenerateChunk(Vector2Int chunk)
        {
            BlockData[,] blocks = new BlockData[16, 16];

            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    Vector2Int POS = ChunkMethods.GlobalBlockCoords(chunk, new Vector2Int(x, y));

                    const double SURFACE_MIN = -20.0;
                    const double SURFACE_MAX = 20.0;

                    const int DIRT_LAYER_SIZE = 3;

                    double rawNoise = NoiseSurface.GetValue(POS.x, 0, 0);
                    double normalizedNoise = (rawNoise + 1.0) / 2.0;

                    int surface_level = Mathf.FloorToInt((float)(normalizedNoise * (SURFACE_MAX - SURFACE_MIN) + SURFACE_MIN));
                    int stone_level = surface_level - DIRT_LAYER_SIZE;
                    int water_level = -1;

                    if (POS.y > surface_level) // air
                    {
                        bool is_lake = POS.y <= (int)water_level;

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
