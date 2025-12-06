using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Core.Utils
{
    public static class GeometryUtils
    {
        public static int ManhattanDistance(Vector2Int v1, Vector2Int v2)
        {
            return Math.Abs(v1.x - v2.x) + Math.Abs(v1.y - v2.y);
        }

        public static float InSquareDistance(Vector2 v1, Vector2 v2)
        {
            return Math.Max(Math.Abs(v1.x - v2.x), Math.Abs(v1.y - v2.y));
        }
    }
}
