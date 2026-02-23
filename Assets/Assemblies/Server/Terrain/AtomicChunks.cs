using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Blocks.All;
using Larnix.Blocks;
using System.Collections.ObjectModel;
using Larnix.Core.DataStructures;
using Larnix.Server.Data;
using Larnix.Core;

namespace Larnix.Server.Terrain
{
    internal class AtomicChunks : IScript
    {
        private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;
        private static readonly Vec2Int WARN_CHUNK = new Vec2Int(int.MinValue, int.MinValue);

        // Config values
        private int MAX_ATOMIC_AREA => Config.MaxElectricContraptionChunks;
        private int WARNING_PERIOD => 750; // 15 seconds at 50 TPS
        private bool WARNING_SUPPRESS => Config.ElectricContraptionSizeWarningSuppress;

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

        private Chunks Chunks => GlobRef.Get<Chunks>();
        private IWorldAPI WorldAPI => GlobRef.Get<IWorldAPI>();
        private Config Config => GlobRef.Get<Config>();

        void IScript.PostEarlyFrameUpdate()
        {
            // Reset info before next frame
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
        /// <exception cref="InvalidOperationException"></exception>
        public IEnumerable<Vec2Int> GetAtomicSet(Vec2Int chunk)
        {
            if (_atomicSets.TryGetGroup(chunk, out var group))
            {
                return group;
            }
            return null;
        }

        /// <summary>
        /// Called from FrameUpdate(), directly after "EventFlag = true" iteration
        /// </summary>
        /// <returns>
        /// True if the whole ISecureAtomic area is loaded.
        /// False if it's possible to make contact with unloaded ISecureAtomic blocks.
        /// </returns>
        public bool IsAtomicLoaded(Vec2Int chunk, HashSet<Vec2Int> wentThrough = null)
        {
            if (!DiscoversChunks)
            {
                throw new InvalidOperationException("Atomic checks cannot be executed in this context!");
            }

            bool rootCall = wentThrough == null;
            if (rootCall)
            {
                if (_atomicLoadedCache.TryGetValue(chunk, out bool cache))
                    return cache; // already computed

                wentThrough = new HashSet<Vec2Int>();
            }

            if (!Chunks.IsChunkLoaded(chunk))
            {
                WishChunk = chunk;
                return false; // unloaded -> false
            }

            bool limitReached = wentThrough.Count >= MAX_ATOMIC_AREA;
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
                        if (!WARNING_SUPPRESS && WorldAPI.ServerTick() % WARNING_PERIOD == 0 && !_warningEmitted)
                        {
                            _warningEmitted = true;
                            Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, Vec2Int.Zero);
                            Core.Debug.LogWarning(
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

                    if (first is not false && other is not false)
                        return true;
                }
            }
            return false;
        }

        private bool? IsActivePropagator(Vec2Int chunk, Vec2Int pos)
        {
            if (Chunks.IsChunkLoaded(chunk))
            {
                Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, pos);

                Block frontBlock = WorldAPI.GetBlock(POS, true);
                Block backBlock = WorldAPI.GetBlock(POS, false);
                
                return frontBlock is ISecureAtomic ||
                    backBlock is ISecureAtomic;
            }
            return null;
        }
    }
}
