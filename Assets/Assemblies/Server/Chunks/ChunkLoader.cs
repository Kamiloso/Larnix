#nullable enable
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Model.Utils;
using Larnix.Server.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Server.Chunks;

internal interface IChunkLoader
{
    public HashSet<Vec2Int> AllStimulatedChunks();
    public Vec2Int? HighestLoadPriority(IEnumerable<Vec2Int> candidates);
}

internal class ChunkLoader : IChunkLoader
{
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();
    private IAtomicChunks AtomicChunks => GlobRef.Get<IAtomicChunks>();

    public HashSet<Vec2Int> AllStimulatedChunks()
    {
        HashSet<Vec2Int> targetLoads = new();

        HashSet<Vec2Int> centers = ConnectedPlayers.AllPlayers
            .Select(nickname => ConnectedPlayers[nickname].RenderPosition)
            .Select(pos => BlockUtils.CoordsToChunk(pos))
            .ToHashSet();

        foreach (Vec2Int center in centers)
        {
            HashSet<Vec2Int> nearbyChunks = BlockUtils.GetNearbyChunks(center, BlockUtils.LOADING_DISTANCE);
            targetLoads.UnionWith(nearbyChunks);
        }

        Vec2Int? wishChunk = AtomicChunks.WishChunk;
        if (wishChunk != null)
        {
            targetLoads.Add(wishChunk.Value);
        }

        return targetLoads;
    }

    public Vec2Int? HighestLoadPriority(IEnumerable<Vec2Int> candidates)
    {
        List<Vec2Int> playerChunks = ConnectedPlayers.AllPlayers
            .Select(nickname => ConnectedPlayers[nickname].RenderPosition)
            .Select(pos => BlockUtils.CoordsToChunk(pos))
            .ToList();

        if (RandUtils.NextBool()) // prevent loading deadlocks
        {
            Vec2Int? wishChunk = AtomicChunks.WishChunk;
            if (wishChunk != null)
            {
                playerChunks.Add(wishChunk.Value);
            }
        }

        if (playerChunks.Count == 0)
            return null;

        Vec2Int? bestChunk = null;
        int bestDistance = int.MaxValue;

        foreach (var candidate in candidates)
        {
            int distMin = int.MaxValue;

            foreach (var playerChunk in playerChunks)
            {
                int dist = Vec2Int.ManhattanDistance(candidate, playerChunk);
                if (dist < distMin) distMin = dist;
                if (distMin == 0) break; // can't get better for this candidate
            }

            if (distMin < bestDistance)
            {
                bestDistance = distMin;
                bestChunk = candidate;
                if (bestDistance == 0) break; // optimal overall
            }
        }

        return bestChunk;
    }
}
