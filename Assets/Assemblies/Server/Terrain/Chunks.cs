#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Larnix.Model.Utils;
using Larnix.Core.Vectors;
using Larnix.Server.Transmission;
using Larnix.Server.Data;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Server.Entities;

namespace Larnix.Server.Terrain;

internal enum ChunkLoadState
{
    None, // not loaded at all, no container
    Loading, // container exists, but chunk instance is not created yet
    Active // chunk instance exists and is being updated, block events are being invoked, etc.
}

internal class Chunks : IScript
{
    private readonly Dictionary<Vec2Int, ChunkContainer> _chunks = new();

    private IClock Clock => GlobRef.Get<IClock>();
    private IEntityControllers EntityControllers => GlobRef.Get<IEntityControllers>();
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();
    private BlockSender BlockSender => GlobRef.Get<BlockSender>();
    private IAtomicChunks AtomicChunks => GlobRef.Get<IAtomicChunks>();
    private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();

    void IScript.EarlyFrameUpdate()
    {
        HashSet<Vec2Int> stimulated = GetStimulatedChunks();

        // Chunk stimulating
        foreach (var chunk in stimulated)
        {
            ChunkStimulate(chunk);
        }

        // Chunk unloading
        HashSet<Vec2Int> toUnload = new();

        foreach (var (chunk, container) in _chunks.ToList())
        {
            if (!stimulated.Contains(chunk))
            {
                container.Tick(Clock.DeltaTime);

                if (container.ShouldUnload(c => _chunks[c]))
                {
                    toUnload.Add(chunk);
                }
            }
        }

        foreach (var chunk in toUnload)
        {
            ChunkUnload(chunk);
        }

        // Chunk activating
        IEnumerable<Vec2Int> loadingChunks = _chunks.Keys
            .Where(ch => IsChunkLoading(ch));

        if (TryGetHighestPriorityLoadingChunk(loadingChunks, out var chunkPos))
        {
            ChunkActivate(chunkPos);
        }
    }

    private void ChunkStimulate(Vec2Int chunk)
    {
        ChunkLoadState state = ChunkState(chunk);
        switch (state)
        {
            case ChunkLoadState.None:
                _chunks[chunk] = new ChunkContainer(chunk);
                EntityControllers.LoadEntityControllersByChunk(chunk);
                return;

            case ChunkLoadState.Loading:
            case ChunkLoadState.Active:
                _chunks[chunk].Stimulate();
                return;
        }
    }

    private void ChunkUnload(Vec2Int chunk)
    {
        ChunkLoadState state = ChunkState(chunk);
        switch(state)
        {
            case ChunkLoadState.Loading:
                _chunks.Remove(chunk);
                return;

            case ChunkLoadState.Active:
                _chunks[chunk].Instance.Dispose();
                _chunks.Remove(chunk);
                return;
        }
    }

    private void ChunkActivate(Vec2Int chunk)
    {
        ChunkLoadState state = ChunkState(chunk);
        switch (state)
        {
            case ChunkLoadState.Loading:
                var chunkObj = new Chunk(chunk);
                _chunks[chunk].Activate(chunkObj);
                return;
        }
    }

    void IScript.FrameUpdate()
    {
        // Invoke block events
        IEnumerator[] invokers = _chunks
            .Where(kv => IsChunkLoaded(kv.Key))
            .OrderBy(kv => kv.Key.y)
            .ThenBy(kv => kv.Key.x)
            .Select(kv => kv.Value.Instance.FrameInvoker)
            .Select(inv => inv.GetEnumerator())
            .ToArray();

        if (invokers.Length >= 1)
        {
            bool first = true;
            bool done = false;

            while (!done)
            {
                if (first)
                {
                    AtomicChunks.DiscoversChunks = true;
                }

                foreach (IEnumerator inv in invokers)
                {
                    bool result = inv.MoveNext();
                    if (!result)
                    {
                        // mark as done, but still continue to let others finish the frame
                        done = true;
                    }
                }

                if (first)
                {
                    AtomicChunks.DiscoversChunks = false;
                    first = false;
                }
            }
        }

        // Updating player chunk data
        if (Clock.FixedFrame % ServerConfig.PeriodicTasks_ChunkSendingPeriodFrames == 0)
        {
            BlockSender.BroadcastChunkChanges();
        }
    }

    public bool IsChunkUnloaded(Vec2Int chunk) =>
        ChunkState(chunk) == ChunkLoadState.None;

    public bool IsChunkLoading(Vec2Int chunk) =>
        ChunkState(chunk) == ChunkLoadState.Loading;

    public bool IsChunkLoaded(Vec2Int chunk) =>
        ChunkState(chunk) == ChunkLoadState.Active;

    public bool IsLoadedPosition(Vec2 position)
    {
        Vec2Int chunk = BlockUtils.CoordsToChunk(position);
        return IsChunkLoaded(chunk);
    }

    public bool IsLoadingPosition(Vec2 position)
    {
        Vec2Int chunk = BlockUtils.CoordsToChunk(position);
        return IsChunkLoading(chunk);
    }

    public bool IsUnloadedPosition(Vec2 position)
    {
        Vec2Int chunk = BlockUtils.CoordsToChunk(position);
        return IsChunkUnloaded(chunk);
    }

    public Chunk? GetChunk(Vec2Int chunk)
    {
        return IsChunkLoaded(chunk)
            ? _chunks[chunk].Instance
            : null;
    }

    public ChunkLoadState ChunkState(Vec2Int chunk)
    {
        if (_chunks.TryGetValue(chunk, out var container))
            return container.State;

        return ChunkLoadState.None;
    }

    private HashSet<Vec2Int> GetStimulatedChunks()
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

    private bool TryGetHighestPriorityLoadingChunk(IEnumerable<Vec2Int> chunkCandidates, out Vec2Int bestChunk)
    {
        bestChunk = default;

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
            return false;

        bool found = false;
        int bestDistance = int.MaxValue;

        foreach (var candidate in chunkCandidates)
        {
            int distMin = int.MaxValue;

            foreach (var pChunk in playerChunks)
            {
                int dist = Vec2Int.ManhattanDistance(candidate, pChunk);
                if (dist < distMin) distMin = dist;
                if (distMin == 0) break; // can't get better for this candidate
            }

            if (distMin < bestDistance)
            {
                bestDistance = distMin;
                bestChunk = candidate;
                found = true;
                if (bestDistance == 0) break; // optimal overall
            }
        }

        return found;
    }
}
