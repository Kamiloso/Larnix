using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Core.Vectors;

namespace Larnix.Core.Physics
{
    internal static class PhysicsUtils
    {
        internal static Vector2Int CoordsToBlock(Vec2 position, double blockSize = 1.0)
        {
            try
            {
                return new Vector2Int(
                    (int)Math.Floor((position.x + 0.5) / blockSize),
                    (int)Math.Floor((position.y + 0.5) / blockSize)
                );
            }
            catch (OverflowException)
            {
                return default;
            }
        }

        internal static Vec2 ChunkCenter(Vector2Int chunkpos)
        {
            return new Vec2(chunkpos.x << 4, chunkpos.y << 4) + new Vec2(7.5, 7.5);
        }
    }
}
