#nullable enable
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Core;
using Larnix.Server.Chunks.Control;

namespace Larnix.Server.Chunks.Scripts;

internal enum ChunkLoadState
{
    Unloaded, // not loaded at all, no container
    Loading, // container exists, but chunk instance is not created yet
    Loaded // chunk instance exists and is being updated, block events are being invoked, etc.
}

internal interface IChunkManager : IScript { }

internal class ChunkManager : IChunkManager
{
    private IClock Clock => GlobRef.Get<IClock>();
    private IChunkLoader ChunkLoader => GlobRef.Get<IChunkLoader>();
    private IChunkHolders ChunkHolders => GlobRef.Get<IChunkHolders>();

    void IScript.EarlyFrameUpdate()
    {
        var stimulated = ChunkLoader.AllStimulatedChunks();

        // Stimulating
        foreach (var chunk in stimulated)
        {
            ChunkHolders.ChunkStimulate(chunk);
        }

        List<Vec2Int> toUnload = new();
        List<Vec2Int> loadingChunks = new();

        foreach (Vec2Int chunk in ChunkHolders.AllChunks)
        {
            ChunkContainer container = ChunkHolders.GetChunkContainer(chunk)!;
            container.AtomicSupportEachOther();

            if (!stimulated.Contains(chunk))
            {
                container.UnloadTick(Clock.DeltaTime);
                if (container.ShouldUnload())
                {
                    toUnload.Add(chunk);
                    continue;
                }
            }

            if (container.State == ChunkLoadState.Loading)
            {
                loadingChunks.Add(chunk);
            }
        }

        // Unloading
        foreach (var chunk in toUnload)
        {
            ChunkHolders.ChunkUnload(chunk);
        }

        // Activating
        Vec2Int? activationChunk = ChunkLoader.HighestLoadPriority(loadingChunks);
        if (activationChunk != null)
        {
            ChunkHolders.ChunkActivate(activationChunk.Value);
        }
    }

    void IScript.FrameUpdate()
    {
        // Invoking block events
        List<IEnumerator> invokers = ChunkHolders.AllInvokers;

        if (invokers.Count >= 1)
        {
            bool done = false;

            while (!done)
            {
                foreach (IEnumerator inv in invokers)
                {
                    if (!inv.MoveNext())
                    {
                        done = true;
                    }
                }
            }
        }
    }
}
