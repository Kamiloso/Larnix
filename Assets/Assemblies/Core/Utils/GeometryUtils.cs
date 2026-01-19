using System;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;

namespace Larnix.Core.Utils
{
    public static class GeometryUtils
    {
        public static int ManhattanDistance(Vec2Int v1, Vec2Int v2)
        {
            long a = Math.Abs(v1.x - v2.x);
            long b = Math.Abs(v1.y - v2.y);
            long result = a + b;
            
            return (int)Math.Min(result, int.MaxValue);
        }
    }
}
