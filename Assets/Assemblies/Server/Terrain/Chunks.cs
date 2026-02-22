using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Larnix.Server.Entities;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Server.Data;
using Larnix.Blocks;

namespace Larnix.Server.Terrain
{
    public enum ChunkLoadState { None, Loading, Active }
    internal class Chunks : IScript
    {
        private Server Server => Ref.Server;
        private PlayerManager PlayerManager => Ref.PlayerManager;
        private EntityManager EntityManager => Ref.EntityManager;
        private BlockSender BlockSender => Ref.BlockSender;
        private AtomicChunks AtomicChunks => Ref.AtomicChunks;
        private Config Config => Ref.Config;

        private readonly Dictionary<Vec2Int, ChunkContainer> _chunks = new();

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
                    container.Tick(Server.RealDeltaTime);

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
            switch(state)
            {
                case ChunkLoadState.None:
                    _chunks[chunk] = new ChunkContainer(chunk);
                    EntityManager.PrepareEntitiesByChunk(chunk);
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
            if (Server.FixedFrame % Config.ChunkSendingPeriodFrames == 0)
            {
                BlockSender.BroadcastChunkChanges();
            }
        }

        public bool IsChunkLoaded(Vec2Int chunk) =>
            ChunkState(chunk) == ChunkLoadState.Active;

        public bool IsChunkLoading(Vec2Int chunk) =>
            ChunkState(chunk) == ChunkLoadState.Loading;

        public bool TryGetChunk(Vec2Int chunk, out Chunk result)
        {
            if (IsChunkLoaded(chunk))
            {
                result = _chunks[chunk].Instance;
                return true;
            }

            result = null;
            return false;
        }

        public Chunk GetChunk(Vec2Int chunk)
        {
            if (TryGetChunk(chunk, out var result))
                return result;

            return null;
        }

        public ChunkLoadState ChunkState(Vec2Int chunk)
        {
            if (_chunks.TryGetValue(chunk, out var container))
                return container.State;
            
            return ChunkLoadState.None;
        }

        public bool IsLoadedPosition(Vec2 position)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(position);
            return IsChunkLoaded(chunk);
        }

        public bool IsEntityInZone(EntityAbstraction entity, ChunkLoadState state)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(entity.ActiveData.Position);
            return ChunkState(chunk) == state;
        }

        private HashSet<Vec2Int> GetStimulatedChunks()
        {
            HashSet<Vec2Int> targetLoads = new();

            IEnumerable<Vec2Int> centers = PlayerManager.AllPlayers()
                .Select(nickname => PlayerManager.RenderingPosition(nickname))
                .Select(pos => BlockUtils.CoordsToChunk(pos));

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

            List<Vec2Int> playerChunks = PlayerManager.AllPlayers()
                .Select(n => BlockUtils.CoordsToChunk(PlayerManager.RenderingPosition(n)))
                .ToList();

            Vec2Int? wishChunk = AtomicChunks.WishChunk;
            if (wishChunk != null)
            {
                playerChunks.Add(wishChunk.Value);
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
                    int dist = GeometryUtils.ManhattanDistance(candidate, pChunk);
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
}
