using UnityEngine;
using Larnix.Core.Vectors;
using System;

namespace Larnix
{
    public static class VectorExtensions
    {
        // === Vec2Int extensions ===

        public static Vec2Int ToLarnix(this Vector2Int v) => new Vec2Int(v.x, v.y);
        public static Vector2Int ToUnity(this Vec2Int v) => new Vector2Int(v.x, v.y);
        public static Vector3Int ToUnity3(this Vec2Int v) => new Vector3Int(v.x, v.y, 0);

        // === Vec2 extensions ===

        public const int ORIGIN_STEP = 16 * 64;

        public static Vec2 ConstructVec2(Vector2 pos, Vec2 origin)
        {
            double _x = origin.x + pos.x;
            double _y = origin.y + pos.y;

            return new Vec2(
                double.IsFinite(_x) ? _x : 0.0,
                double.IsFinite(_y) ? _y : 0.0
            );
        }

        public static Vec2 ExtractOrigin(this Vec2 v)
        {
            Vec2Int middleblock = ORIGIN_STEP * v.ExtractSector() + ORIGIN_STEP / 2 * Vec2Int.One;
            return new Vec2(
                middleblock.x - 0.5,
                middleblock.y - 0.5
                );
        }

        public static Vector2 ExtractPosition(this Vec2 v, Vec2 origin)
        {
            return new Vector2(
                (float)(v.x - origin.x),
                (float)(v.y - origin.y)
            );
        }

        public static Vec2Int ExtractSector(this Vec2 v)
        {
            try
            {
                return new Vec2Int(
                    (int)Math.Floor((v.x + 0.5) / ORIGIN_STEP),
                    (int)Math.Floor((v.y + 0.5) / ORIGIN_STEP)
                );
            }
            catch (OverflowException)
            {
                return default;
            }
        }
    }
}
