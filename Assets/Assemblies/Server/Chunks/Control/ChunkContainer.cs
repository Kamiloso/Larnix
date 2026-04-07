#nullable enable
using Larnix.Core.Vectors;
using System;
using System.Collections.Generic;
using Larnix.Core;
using Larnix.Server.Chunks.Scripts;
using Larnix.Server.Chunks.Data;

namespace Larnix.Server.Chunks.Control;

internal class ChunkContainer
{
    private const float UNLOAD_TIME = 2f; // seconds

    public Vec2Int Chunkpos { get; }
    public ChunkLoadState State { get; private set; }
    public ChunkBrain? Instance { get; private set; }
    public float TimeToUnload { get; private set; }

    private List<Vec2Int> AtomicGroup => AtomicChunks.GetAtomicSet(Chunkpos) ?? new List<Vec2Int> { Chunkpos };
    private Func<Vec2Int, ChunkContainer> GetChunk { get; }

    private IChunkRepository ChunkRepository => GlobRef.Get<IChunkRepository>();
    private IAtomicChunks AtomicChunks => GlobRef.Get<IAtomicChunks>();

    public ChunkContainer(Vec2Int chunkpos, Func<Vec2Int, ChunkContainer> getChunk)
    {
        Chunkpos = chunkpos;
        State = ChunkLoadState.Loading;
        TimeToUnload = UNLOAD_TIME;
        GetChunk = getChunk;
    }

    public void Activate()
    {
        if (Instance != null)
            throw new InvalidOperationException($"Chunk {Chunkpos} already initialized.");

        State = ChunkLoadState.Loaded;
        Instance = new ChunkBrain(Chunkpos, ChunkRepository.TakeActiveChunk(Chunkpos));
    }

    public void UnloadTick(float deltaTime) => TimeToUnload -= deltaTime;
    public void Stimulate() => TimeToUnload = UNLOAD_TIME;

    public void AtomicSupportEachOther()
    {
        foreach (Vec2Int chunk in AtomicGroup)
        {
            ChunkContainer other = GetChunk(chunk);
            other.TimeToUnload = Math.Max(other.TimeToUnload, TimeToUnload);
        }
    }

    public bool ShouldUnload()
    {
        List<Vec2Int> atomicGroup = AtomicGroup;
        return atomicGroup.TrueForAll(chunk => GetChunk(chunk).TimeToUnload <= 0f);
    }

    public void Dispose()
    {
        if (Instance is not null)
        {
            Instance.Dispose();
            ChunkRepository.ReturnActiveChunk(Chunkpos);
        }
    }
}
