using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Blocks
{
    public static class ChunkMethods
    {
        public static Vector2Int CoordsToChunk(Vector2 floatPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt((floatPosition.x + 0.5f) / 16),
                Mathf.FloorToInt((floatPosition.y + 0.5f) / 16)
                );
        }

        public static Vector2Int CoordsToChunk(Vector2Int intPosition)
        {
            return intPosition / 16;
        }

        public static Vector2Int CoordsToBlock(Vector2 floatPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(floatPosition.x + 0.5f),
                Mathf.FloorToInt(floatPosition.y + 0.5f)
                );
        }

        public static Vector2Int GlobalBlockCoords(Vector2Int chunkpos, Vector2Int pos)
        {
            return new Vector2Int(chunkpos.x << 4, chunkpos.y << 4) + pos;
        }

        public static Vector2Int LocalBlockCoords(Vector2Int POS)
        {
            int x = POS.x & 0b1111;
            int y = POS.y & 0b1111;
            return new Vector2Int(x, y);
        }

        public static bool InChunk(Vector2Int chunkpos, Vector2Int POS)
        {
            return (POS.x >> 4) == chunkpos.x && (POS.y >> 4) == chunkpos.y;
        }
    }
}
