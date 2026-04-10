#nullable enable
using Larnix.Core.Vectors;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Larnix.Model.Utils;

public static class ColliderUtils
{
    public static Vec2 MaxColliderSize => new(6f, 6f);

    // TODO: Make colliders more robust. Instead of assuming that every
    // collider has a max size, we should implement a more complex size
    // deduction system based on a given EntityData.
    //
    // Following:
    // - Inside PhysicsManager modify alghorithm to check EVERY collider that
    //   moving entity may collide with and not just a simple heuristic.

    static ColliderUtils()
    {
        if (MaxColliderSize.x <= 0 || MaxColliderSize.x >= BlockUtils.CHUNK_SIZE ||
            MaxColliderSize.y <= 0 || MaxColliderSize.y >= BlockUtils.CHUNK_SIZE)
        {
            throw new ArgumentException($"Max collider size {MaxColliderSize} is invalid.");
        }
    }

    public static void AssertSizePositive(Vec2 size)
    {
        if (size.x <= 0 || size.y <= 0)
        {
            throw new ArgumentException($"Collider size {size} must be positive.");
        }
    }

    public static void AssertSizeWithinLimits(Vec2 size, Vec2 offset)
    {
        double mx = size.x / 2.0 + Math.Abs(offset.x);
        double my = size.y / 2.0 + Math.Abs(offset.y);

        if (mx > MaxColliderSize.x || my > MaxColliderSize.y)
        {
            throw new ArgumentException(
                $"Collider size {size} with offset {offset} exceeds the max size {MaxColliderSize}.");
        }
    }

    public static bool CenterInChunk(Vec2 position, Vec2Int chunk)
    {
        Vec2Int entityChunk = BlockUtils.CoordsToChunk(position);
        return entityChunk == chunk;
    }

    public static bool CenterInAnyChunk(Vec2 position, HashSet<Vec2Int> chunks)
    {
        Vec2Int entityChunk = BlockUtils.CoordsToChunk(position);
        return chunks.Contains(entityChunk);
    }

    public static bool InActiveChunks(Vec2 position, HashSet<Vec2Int> chunks)
    {
        Vec2 d0 = MaxColliderSize / 2.0;

        Vec2[] corners = new Vec2[]
        {
            position + new Vec2(d0.x, d0.y),
            position + new Vec2(d0.x, -d0.y),
            position + new Vec2(-d0.x, d0.y),
            position + new Vec2(-d0.x, -d0.y),
        };

        return corners.All(corner => CenterInAnyChunk(corner, chunks));
    }
}
