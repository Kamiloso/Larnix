#nullable enable
using System;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;
using Larnix.Model.Utils;

namespace Larnix.Model.Blocks;

public enum IterationOrder { XY, YX, Random }

public static class ChunkIterator
{
    private const int C_16 = BlockUtils.CHUNK_SIZE;

    public static T[,] Array16x16<T>(Func<T>? fill = null)
    {
        T[,] array = new T[C_16, C_16];

        if (fill is null)
        {
            return array;
        }

        Iterate((x, y) => array[x, y] = fill.Invoke());
        return array;
    }

    public static void Iterate(Action<int, int> action, IterationOrder order = IterationOrder.XY)
    {
        Action<Action<int, int>> iteration = order switch
        {
            IterationOrder.XY => IterateXY,
            IterationOrder.YX => IterateYX,
            IterationOrder.Random => IterateRandom,
            _ => throw new InvalidOperationException("Invalid iteration order: " + order)
        };

        iteration.Invoke(action);
    }

    public static int Compare(Vec2Int a, Vec2Int b, IterationOrder order, bool suppressException = false)
    {
        return order switch
        {
            IterationOrder.XY => a.x != b.x ? a.x - b.x : a.y - b.y,
            IterationOrder.YX => a.y != b.y ? a.y - b.y : a.x - b.x,
            _ => suppressException ? 0 : throw new InvalidOperationException("Cannot compare positions in " + order + " order!")
        };
    }

    public static void IterateWithPOS(Vec2Int chunk, Action<Vec2Int, int, int> action, IterationOrder order = IterationOrder.XY)
    {
        Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, Vec2Int.Zero);
        Iterate((x, y) => action(POS + new Vec2Int(x, y), x, y), order);
    }

    private static void IterateXY(Action<int, int> action)
    {
        for (int x = 0; x < C_16; x++)
            for (int y = 0; y < C_16; y++)
            {
                action(x, y);
            }
    }

    private static void IterateYX(Action<int, int> action)
    {
        for (int y = 0; y < C_16; y++)
            for (int x = 0; x < C_16; x++)
            {
                action(x, y);
            }
    }

    private static void IterateRandom(Action<int, int> action)
    {
        Span<int> indexes = stackalloc int[C_16 * C_16];
        for (int i = 0; i < indexes.Length; i++)
        {
            indexes[i] = i;
        }

        var rng = RandUtils.Rand;
        int n = indexes.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (indexes[k], indexes[n]) = (indexes[n], indexes[k]);
        }

        foreach (int index in indexes)
        {
            action.Invoke(index % C_16, index / C_16);
        }
    }
}
