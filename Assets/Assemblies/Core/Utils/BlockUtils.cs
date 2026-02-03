using Larnix.Core.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Larnix.Core.Utils
{
    public static class BlockUtils
    {
        public const int CHUNK_SIZE = 16;

        public const int MIN_CHUNK = -(1 << 27) + 1;
        public const int MAX_CHUNK = -(MIN_CHUNK + 1);

        public const int MIN_BLOCK = MIN_CHUNK * 16;
        public const int MAX_BLOCK = -(MIN_BLOCK + 1);

        public const int LOADING_DISTANCE = 2; // chunks

        public static Vec2Int CoordsToChunk(Vec2 position)
        {
            return CoordsToBlock(position, 16.0);
        }

        public static Vec2Int CoordsToChunk(Vec2Int intPosition)
        {
            return new Vec2Int(
                intPosition.x >> 4,
                intPosition.y >> 4
                );
        }

        public static Vec2Int CoordsToBlock(Vec2 position, double blockSize = 1.0)
        {
            try
            {
                return new Vec2Int(
                    (int)Math.Floor((position.x + 0.5) / blockSize),
                    (int)Math.Floor((position.y + 0.5) / blockSize)
                );
            }
            catch (OverflowException)
            {
                return default;
            }
        }

        public static Vec2Int GlobalBlockCoords(Vec2Int chunkpos, Vec2Int pos)
        {
            return new Vec2Int(chunkpos.x << 4, chunkpos.y << 4) + pos;
        }

        public static Vec2Int LocalBlockCoords(Vec2Int POS)
        {
            int x = POS.x & 0b1111;
            int y = POS.y & 0b1111;
            return new Vec2Int(x, y);
        }

        public static bool InChunk(Vec2Int chunkpos, Vec2Int POS)
        {
            return (POS.x >> 4) == chunkpos.x && (POS.y >> 4) == chunkpos.y;
        }

        public static HashSet<Vec2Int> GetCenterChunks(List<Vec2> positions)
        {
            HashSet<Vec2Int> returns = new();
            foreach (Vec2 pos in positions)
            {
                returns.Add(CoordsToChunk(pos));
            }
            return returns;
        }

        public static HashSet<Vec2Int> GetNearbyChunks(Vec2Int center, int simDistance)
        {
            int min_x = Math.Clamp(center.x - simDistance, MIN_CHUNK, int.MaxValue);
            int min_y = Math.Clamp(center.y - simDistance, MIN_CHUNK, int.MaxValue);
            int max_x = Math.Clamp(center.x + simDistance, int.MinValue, MAX_CHUNK);
            int max_y = Math.Clamp(center.y + simDistance, int.MinValue, MAX_CHUNK);

            HashSet<Vec2Int> returns = new();
            for (int x = min_x; x <= max_x; x++)
                for (int y = min_y; y <= max_y; y++)
                {
                    returns.Add(new Vec2Int(x, y));
                }

            return returns;
        }

        public static Vec2 ChunkCenter(Vec2Int chunkpos)
        {
            return new Vec2(chunkpos.x << 4, chunkpos.y << 4) + new Vec2(7.5, 7.5);
        }
    }
}
