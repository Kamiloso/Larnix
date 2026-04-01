#nullable enable
using System;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;

namespace Larnix.Model.Utils;

public enum IterationOrder { XY, YX, Random }
public static class ChunkIterator
{
    private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;
    public static T[,] Array2D<T>() => new T[CHUNK_SIZE, CHUNK_SIZE];

    public static void Iterate(Action<int, int> action, IterationOrder order = IterationOrder.XY)
    {
        switch (order)
        {
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
        Span<int> indexes = stackalloc int[CHUNK_SIZE * CHUNK_SIZE];
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
            action(index % CHUNK_SIZE, index / CHUNK_SIZE);
        }
    }
}
