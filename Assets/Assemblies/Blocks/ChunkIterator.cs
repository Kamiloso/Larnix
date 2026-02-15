using System;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;

namespace Larnix.Blocks
{
    public enum IterationOrder { Any, XY, YX, Random }
    public static class ChunkIterator
    {
        private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;

        public static void Iterate(Action<int, int> action, IterationOrder order = IterationOrder.Any)
        {
            switch (order)
            {
                case IterationOrder.Any:
                case IterationOrder.XY:
                    IterateXY(action);
                    break;
                
                case IterationOrder.YX:
                    IterateYX(action);
                    break;

                case IterationOrder.Random:
                    IterateRandom(action);
                    break;
            }
        }

        public static int Compare(Vec2Int a, Vec2Int b, IterationOrder order)
        {
            return order switch
            {
                IterationOrder.Any => Compare(a, b, IterationOrder.XY),
                IterationOrder.XY => a.x != b.x ? a.x - b.x : a.y - b.y,
                IterationOrder.YX => a.y != b.y ? a.y - b.y : a.x - b.x,
                _ => 0
            };
        }

        private static void IterateXY(Action<int, int> action)
        {
            for (int x = 0; x < CHUNK_SIZE; x++)
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    action(x, y);
                }
        }

        private static void IterateYX(Action<int, int> action)
        {
            for (int y = 0; y < CHUNK_SIZE; y++)
                for (int x = 0; x < CHUNK_SIZE; x++)
                {
                    action(x, y);
                }
        }

        private static void IterateRandom(Action<int, int> action)
        {
            var indexes = new int[CHUNK_SIZE * CHUNK_SIZE];
            for (int i = 0; i < indexes.Length; i++)
            {
                indexes[i] = i;
            }

            var rng = Common.Rand();
            int n = indexes.Length;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (indexes[k], indexes[n]) = (indexes[n], indexes[k]);
            }

            foreach (int index in indexes)
            {
                action(index % CHUNK_SIZE, index / CHUNK_SIZE);
            }
        }
    }
}