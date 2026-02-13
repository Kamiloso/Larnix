using System;
using System.Collections.Generic;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;

namespace Larnix.Blocks
{
    public static class ChunkIterator
    {
        public enum Order { Any, XY, YX, Random }
        private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;

        public static IEnumerable<Vec2Int> Iterate(Order order)
        {
            return order switch
            {
                Order.Any => IterateXY(),
                Order.XY => IterateXY(),
                Order.YX => IterateYX(),
                Order.Random => IterateRandom(),
                _ => throw new ArgumentOutOfRangeException("Invalid iteration order!")
            };
        }

        public static IEnumerable<Vec2Int> IterateXY()
        {
            for (int x = 0; x < CHUNK_SIZE; x++)
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    yield return new Vec2Int(x, y);
                }
        }

        public static IEnumerable<Vec2Int> IterateYX()
        {
            for (int y = 0; y < CHUNK_SIZE; y++)
                for (int x = 0; x < CHUNK_SIZE; x++)
                {
                    yield return new Vec2Int(x, y);
                }
        }

        public static IEnumerable<Vec2Int> IterateRandom()
        {
            var positions = new List<Vec2Int>(CHUNK_SIZE * CHUNK_SIZE);
            for (int x = 0; x < CHUNK_SIZE; x++)
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    positions.Add(new Vec2Int(x, y));
                }

            var rng = Common.Rand();
            int n = positions.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (positions[k], positions[n]) = (positions[n], positions[k]);
            }

            foreach (var pos in positions)
            {
                yield return pos;
            }
        }
    }
}