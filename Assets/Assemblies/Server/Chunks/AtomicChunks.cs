#nullable enable
using System;
using System.Collections.Generic;
using Larnix.Model.Utils;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.All;
using Larnix.Model.Blocks;
using System.Collections.ObjectModel;
using Larnix.Core.Collections;
using Larnix.Server.Data;
using Larnix.Core;
using Larnix.Server.Chunks.Scripts;

namespace Larnix.Server.Chunks;

internal interface IAtomicChunks
{
    Vec2Int? WishChunk { get; }
    bool DiscoversChunks { get; set; }

    void Reset();
    List<Vec2Int>? GetAtomicSet(Vec2Int chunk);
    bool IsAtomicLoaded(Vec2Int chunk, HashSet<Vec2Int>? wentThrough = null);
}

internal class AtomicChunks : IAtomicChunks
{
    private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;
    private static Vec2Int WARN_CHUNK => new(int.MinValue, int.MinValue);

    private int MaxAtomicArea => ServerConfig.Electricity_MaxContraptionChunks;
    private int WarningPeriod => 750; // 15 seconds at 50 TPS
    private bool WarningSuppress => ServerConfig.Electricity_SizeWarningSuppress;

    const int MIN = 0;
    const int MAX = CHUNK_SIZE - 1;

    private static readonly ReadOnlyDictionary<Vec2Int, Func<int, (Vec2Int First, Vec2Int Other)>> _chunkDirPos = new(
        new Dictionary<Vec2Int, Func<int, (Vec2Int First, Vec2Int Other)>>
        {
            [Vec2Int.Up] = x => (
                First: new Vec2Int(x, MAX),
                Other: new Vec2Int(x, MIN)
            ),
            [Vec2Int.Right] = x => (
                First: new Vec2Int(MAX, x),
                Other: new Vec2Int(MIN, x)
            ),
            [Vec2Int.Down] = x => (
                First: new Vec2Int(x, MIN),
                Other: new Vec2Int(x, MAX)
            ),
            [Vec2Int.Left] = x => (
                First: new Vec2Int(MIN, x),
                Other: new Vec2Int(MAX, x)
            ),
    });

    private readonly Dictionary<Vec2Int, bool> _atomicLoadedCache = new();
    private readonly GroupSet<Vec2Int> _atomicSets = new(groupsAlwaysDistinct: true);
    private bool _warningEmitted = false;

    public Vec2Int? WishChunk { get; private set; } = null;
    public bool DiscoversChunks { get; set; } = false;

    private IChunkHolders ChunkHolders => GlobRef.Get<IChunkHolders>();
    private IWorldAPI WorldAPI => GlobRef.Get<IWorldAPI>();
    private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();

    public void Reset()
    {
        _atomicLoadedCache.Clear();
        _atomicSets.Clear();
        _warningEmitted = false;

        WishChunk = null;
    }

    /// <summary>
    /// Called from EarlyFrameUpdate()
    /// </summary>
    /// <returns>
    /// List of all chunks that should be unloaded along with the given chunk.
    /// Null if no atomic set is associated with the chunk.
    /// </returns>
    public List<Vec2Int>? GetAtomicSet(Vec2Int chunk)
    {
        return _atomicSets.TryGetGroup(chunk, out var group) ? group : null;
    }

    /// <summary>
    /// Called from FrameUpdate(), directly after "EventFlag = true" iteration
    /// </summary>
    /// <returns>
    /// True if the whole ISecureAtomic area is loaded.
    /// False if it's possible to make contact with unloaded ISecureAtomic blocks.
    /// </returns>
    public bool IsAtomicLoaded(Vec2Int chunk, HashSet<Vec2Int>? wentThrough = null)
    {
        if (!DiscoversChunks)
        {
            throw new InvalidOperationException("Atomic checks cannot be executed in this context!");
        }

        bool rootCall = wentThrough is null;
        if (rootCall)
        {
            if (_atomicLoadedCache.TryGetValue(chunk, out bool cache))
            {
                return cache; // already computed
            }

            wentThrough = new HashSet<Vec2Int>();
        }

        if (!ChunkHolders.IsChunkInZone(chunk, ChunkLoadState.Loaded))
        {
            WishChunk = chunk;
            return false; // not loaded -> false
        }

        bool limitReached = wentThrough!.Count >= MaxAtomicArea;
        if (limitReached)
        {
            wentThrough.Add(WARN_CHUNK);
            return false; // prevent infinite recursion
        }

        // At this point, chunk is definitely in the atomic area.
        // From here on chunk is loaded and will be included in cache if root call.
        wentThrough.Add(chunk);

        bool Return(bool value)
        {
            if (rootCall)
            {
                foreach (Vec2Int ch in wentThrough)
                {
                    _atomicLoadedCache[ch] = value;
                }

                bool tooLarge = wentThrough.Contains(WARN_CHUNK);

                if (tooLarge)
                {
                    if (!WarningSuppress && WorldAPI.ServerTick % WarningPeriod == 0 && !_warningEmitted)
                    {
                        _warningEmitted = true;
                        Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, Vec2Int.Zero);
                        Echo.LogWarning(
                            $"Electric contraption at {POS} is too large to be fully loaded!\n" +
                            $"Its events will be disabled to prevent inconsistent behaviour.\n" +
                            $"You can increase the limit or suppress this warning in the server config."
                        );
                        return false;
                    }
                }
                else
                {
                    _atomicSets.AddGroup(wentThrough);
                }
            }
            return value;
        }

        bool result = true;

        foreach (Vec2Int dir in Vec2Int.CardinalDirections)
        {
            Vec2Int neighbor = chunk + dir;

            if (!ChunkMayPropagate(chunk, dir))
                continue;

            if (!BlockUtils.ChunkExists(neighbor))
                continue;

            if (wentThrough.Contains(neighbor))
                continue;

            if (!IsAtomicLoaded(neighbor, wentThrough))
                result = false;
        }

        return Return(result);
    }

    private bool ChunkMayPropagate(Vec2Int chunk, Vec2Int direction)
    {
        Vec2Int neighbor = chunk + direction;
        if (BlockUtils.ChunkExists(neighbor))
        {
            for (int i = MIN; i <= MAX; i++)
            {
                bool? first = IsActivePropagator(chunk, _chunkDirPos[direction](i).First);
                bool? other = IsActivePropagator(neighbor, _chunkDirPos[direction](i).Other);

                if (first != false && other != false)
                    return true;
            }
        }
        return false;
    }

    private bool? IsActivePropagator(Vec2Int chunk, Vec2Int pos)
    {
        if (ChunkHolders.IsChunkInZone(chunk, ChunkLoadState.Loaded))
        {
            Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, pos);

            Block? frontBlock = WorldAPI.GetBlock(POS, true);
            Block? backBlock = WorldAPI.GetBlock(POS, false);

            return frontBlock is ISecureAtomic || backBlock is ISecureAtomic;
        }
        return null;
    }
}
