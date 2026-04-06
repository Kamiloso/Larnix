#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Utils;
using Larnix.Server.Chunks.Control;
using Larnix.Server.Chunks.Scripts;
using Larnix.Server.Packets.Structs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Server.Chunks;

internal interface IChunkHolders
{
    event Action<Vec2Int>? OnStartedLoading;
    event Action<Vec2Int>? OnFullyLoaded;
    event Action<Vec2Int>? OnUnloaded;

    public List<Vec2Int> AllChunks { get; }
    public List<IEnumerator> AllInvokers { get; }

    public void ChunkStimulate(Vec2Int chunk);
    public void ChunkUnload(Vec2Int chunk);
    public void ChunkActivate(Vec2Int chunk);

    bool IsChunkInZone(Vec2Int chunk, ChunkLoadState state);
    bool IsPositionInZone(Vec2 position, ChunkLoadState state);

    ChunkContainer? GetChunkContainer(Vec2Int chunk);
    ChunkBrain? GetChunkBrain(Vec2Int chunk);
    ChunkLoadState ChunkState(Vec2Int chunk);
}

internal class ChunkHolders : IChunkHolders
{
    private readonly Dictionary<Vec2Int, ChunkContainer> _chunks = new();

    public event Action<Vec2Int>? OnStartedLoading;
    public event Action<Vec2Int>? OnFullyLoaded;
    public event Action<Vec2Int>? OnUnloaded;

    public List<Vec2Int> AllChunks => _chunks.Keys.ToList();
    public List<IEnumerator> AllInvokers => _chunks
        .Where(kv => IsChunkInZone(kv.Key, ChunkLoadState.Loaded))
        .OrderBy(kv => kv.Key.y)
        .ThenBy(kv => kv.Key.x)
        .Select(kv => kv.Value.Instance!.FrameInvoker)
        .Select(inv => inv.GetEnumerator())
        .ToList();

    public void ChunkStimulate(Vec2Int chunk)
    {
        ChunkLoadState state = ChunkState(chunk);
        if (state == ChunkLoadState.Unloaded)
        {
            _chunks[chunk] = new ChunkContainer(chunk, c => _chunks[c]);
            OnStartedLoading?.Invoke(chunk);
        }
        else
        {
            _chunks[chunk].Stimulate();
        }
    }

    public void ChunkUnload(Vec2Int chunk)
    {
        ChunkLoadState state = ChunkState(chunk);
        if (state == ChunkLoadState.Loading)
        {
            _chunks.Remove(chunk);
            OnUnloaded?.Invoke(chunk);
        }
        else if (state == ChunkLoadState.Loaded)
        {
            _chunks[chunk].Dispose();
            _chunks.Remove(chunk);
            OnUnloaded?.Invoke(chunk);
        }
    }

    public void ChunkActivate(Vec2Int chunk)
    {
        ChunkLoadState state = ChunkState(chunk);
        if (state == ChunkLoadState.Unloaded)
        {
            ChunkStimulate(chunk);
            ChunkActivate(chunk);
        }
        else if (state == ChunkLoadState.Loading)
        {
            _chunks[chunk].Activate();
            OnFullyLoaded?.Invoke(chunk);
        }
    }

    public bool IsChunkInZone(Vec2Int chunk, ChunkLoadState state)
    {
        return ChunkState(chunk) == state;
    }

    public bool IsPositionInZone(Vec2 position, ChunkLoadState state)
    {
        Vec2Int chunk = BlockUtils.CoordsToChunk(position);
        return IsChunkInZone(chunk, state);
    }

    public ChunkContainer? GetChunkContainer(Vec2Int chunk)
    {
        return _chunks.TryGetValue(chunk, out var container)
            ? container : null;
    }

    public ChunkBrain? GetChunkBrain(Vec2Int chunk)
    {
        return GetChunkContainer(chunk)?.Instance;
    }

    public ChunkLoadState ChunkState(Vec2Int chunk)
    {
        return GetChunkContainer(chunk)?.State ?? ChunkLoadState.Unloaded;
    }
}
